import sqlite3
import os
import json

def get_easy_words(c, cat_target, cat_other, limit=1):
    # We want words where cat_target is the strongest, and it's much stronger than cat_other
    query = f"""
    SELECT word, {cat_target}, {cat_other}
    FROM scores
    WHERE {cat_target} > 0.35
      AND {cat_target} > {cat_other} + 0.15
      AND {cat_target} = MAX(dawn, nocturne, hearth, frost, canopy)
    ORDER BY {cat_target} DESC
    LIMIT {limit}
    """
    c.execute(query)
    return [{"word": row[0], "score": row[1], "target": cat_target.capitalize()} for row in c.fetchall()]

def get_tricky_words(c, cat1, cat2, threshold, limit=4):
    # We want words where cat1 and cat2 are both high and very close
    # AND they are the top two categories overall.
    # A simple way in SQL:
    # cat1 > threshold AND cat2 > threshold AND ABS(cat1 - cat2) < 0.05
    # AND cat1 > ALL_OTHERS AND cat2 > ALL_OTHERS
    
    cats = ["dawn", "nocturne", "hearth", "frost", "canopy"]
    others = [cat for cat in cats if cat not in (cat1, cat2)]
    
    # ensure cat1 and cat2 are greater than all others
    other_conditions = " AND ".join([f"{cat1} > {o} AND {cat2} > {o}" for o in others])
    
    query = f"""
    SELECT word, {cat1}, {cat2}
    FROM scores
    WHERE {cat1} > {threshold}
      AND {cat2} > {threshold}
      AND ABS({cat1} - {cat2}) < 0.05
      AND {other_conditions}
    ORDER BY ({cat1} + {cat2}) DESC
    LIMIT {limit}
    """
    c.execute(query)
    return [{"word": row[0], f"{cat1}_score": round(row[1],3), f"{cat2}_score": round(row[2],3)} for row in c.fetchall()]

def build_round(c, cat1, cat2, tricky_threshold):
    cat1 = cat1.lower()
    cat2 = cat2.lower()
    
    easy1 = get_easy_words(c, cat1, cat2, limit=1)
    easy2 = get_easy_words(c, cat2, cat1, limit=1)
    
    tricky = get_tricky_words(c, cat1, cat2, tricky_threshold, limit=4)
    
    return {
        "round": f"{cat1.capitalize()} vs {cat2.capitalize()}",
        "easy_words": easy1 + easy2,
        "tricky_words": tricky
    }

def main():
    # Try full DB first, fallback to 10k DB
    db_path = 'semantic_scores_full.db' if os.path.exists('semantic_scores_full.db') else 'semantic_scores.db'
    print(f"Using database: {db_path}")
    
    conn = sqlite3.connect(db_path)
    c = conn.cursor()
    
    # We'll test a few tricky thresholds to see what yields 4 words
    rounds_config = [
        ("Dawn", "Hearth"),
        ("Nocturne", "Canopy"),
        ("Frost", "Dawn")
    ]
    
    results = []
    for cat1, cat2 in rounds_config:
        # Auto-tune tricky threshold: Start high, lower until we get 4 words
        threshold = 0.40
        round_data = None
        while threshold >= 0.15:
            round_data = build_round(c, cat1, cat2, threshold)
            if len(round_data["tricky_words"]) >= 4:
                print(f"[{cat1} vs {cat2}] Found 4 tricky words at threshold {threshold:.2f}")
                break
            threshold -= 0.02
            
        results.append(round_data)
        
    print("\n--- PROPOSED ROUNDS ---")
    print(json.dumps(results, indent=2))
    
    with open("rounds_export.json", "w") as f:
        json.dump(results, f, indent=2)
        
    conn.close()

if __name__ == "__main__":
    main()
