import sqlite3
import numpy as np
from sentence_transformers import SentenceTransformer
import requests
import time

def main():
    start_time = time.time()
    
    # 0. Download stopwords to filter out words like "the", "for", "that"
    print("Downloading stopwords...")
    sw_url = "https://raw.githubusercontent.com/igorbrigadir/stopwords/master/en/spacy.txt"
    sw_resp = requests.get(sw_url)
    stopwords = set(sw_resp.text.splitlines())

    # 1. Download FULL English dictionary (~370k words)
    print("Downloading full English dictionary...")
    url = "https://raw.githubusercontent.com/dwyl/english-words/master/words_alpha.txt"
    resp = requests.get(url)
    resp.raise_for_status()
    
    # Read words, discard very short ones and stopwords
    words = [w for w in resp.text.splitlines() if len(w) > 2 and w not in stopwords]
    print(f"Loaded {len(words)} valid words (filtered out stopwords).")

    # 2. Setup model
    print("Loading model 'all-mpnet-base-v2'...")
    model = SentenceTransformer('all-mpnet-base-v2')

    categories = {
        "Dawn": ["light", "sunrise", "awakening", "sky", "morning"],
        "Nocturne": ["shadow", "void", "sleep", "mystery", "midnight"],
        "Hearth": ["fire", "warmth", "home", "civilization", "embers"],
        "Frost": ["cold", "ice", "stillness", "mist", "winter"],
        "Canopy": ["flora", "roots", "earth", "growth", "beasts"]
    }

    print("Embedding categories...")
    cat_vectors = {}
    for cat, desc_words in categories.items():
        embs = model.encode(desc_words)
        avg_emb = np.mean(embs, axis=0)
        avg_emb = avg_emb / np.linalg.norm(avg_emb)
        cat_vectors[cat] = avg_emb

    print("Embedding candidate words (this will take a few minutes)...")
    # show_progress_bar=True will print a tqdm progress bar to the console!
    word_embs = model.encode(words, show_progress_bar=True)
    word_embs = word_embs / np.linalg.norm(word_embs, axis=1, keepdims=True)

    # 3. Calculate similarities & store in DB
    print("Calculating similarities and storing to SQLite DB...")
    db_path = 'semantic_scores_full.db'
    conn = sqlite3.connect(db_path)
    c = conn.cursor()
    c.execute('''
        CREATE TABLE IF NOT EXISTS scores (
            word TEXT PRIMARY KEY,
            dawn REAL,
            nocturne REAL,
            hearth REAL,
            frost REAL,
            canopy REAL
        )
    ''')
    c.execute('DELETE FROM scores')

    category_names = ["Dawn", "Nocturne", "Hearth", "Frost", "Canopy"]
    cat_matrix = np.array([cat_vectors[name] for name in category_names]).T 

    similarities = np.dot(word_embs, cat_matrix)

    # Insert in batches to be safe with memory
    batch_size = 50000
    for i in range(0, len(words), batch_size):
        records = []
        batch_words = words[i:i+batch_size]
        batch_sims = similarities[i:i+batch_size]
        for j, word in enumerate(batch_words):
            scores = batch_sims[j]
            records.append((word, float(scores[0]), float(scores[1]), float(scores[2]), float(scores[3]), float(scores[4])))
        c.executemany('INSERT INTO scores VALUES (?, ?, ?, ?, ?, ?)', records)
        conn.commit()

    # 4. Evaluate heuristics
    easy_words = []
    tricky_words = []
    impossible_count = 0

    for i, word in enumerate(words):
        scores = similarities[i]
        max_s = np.max(scores)
        min_s = np.min(scores)
        
        sorted_scores = np.sort(scores)[::-1]
        s1 = sorted_scores[0]
        s2 = sorted_scores[1]
        
        if max_s < 0.25:
            impossible_count += 1
        else:
            is_tricky = (s1 > 0.40 and s2 > 0.35 and (s1 - s2) < 0.05)
            is_easy = (s1 > 0.40 and (s1 - s2) > 0.15)
            
            if is_tricky:
                tricky_words.append(word)
            elif is_easy:
                easy_words.append(word)

    print("\n--- Heuristics Results ---")
    print(f"Total Words Tested: {len(words)}")
    print(f"Impossible (max < 0.25): {impossible_count}")
    print(f"Easy Criteria Matched: {len(easy_words)}")
    print(f"Tricky Criteria Matched: {len(tricky_words)}")

    conn.close()
    
    elapsed = time.time() - start_time
    print(f"\nDone in {elapsed:.1f} seconds! DB saved at {db_path}")

if __name__ == "__main__":
    main()
