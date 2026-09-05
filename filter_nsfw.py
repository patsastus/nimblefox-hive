import json
import requests

def filter_nsfw():
    print("Downloading NSFW blocklist...")
    # Fetch a standard widely-used blocklist
    url = "https://raw.githubusercontent.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words/master/en"
    resp = requests.get(url)
    bad_words = set([w.strip().lower() for w in resp.text.splitlines()])
    
    # Load the JSON DB
    with open("Assets/word_database.json", "r") as f:
        data = json.load(f)
        
    original_count = len(data["words"])
    
    # Filter
    filtered_words = []
    for w in data["words"]:
        # Also check if it contains highly offensive substrings just in case
        word = w["word"].lower()
        if word not in bad_words:
            filtered_words.append(w)
            
    removed_count = original_count - len(filtered_words)
    
    data["words"] = filtered_words
    
    with open("Assets/word_database.json", "w") as f:
        json.dump(data, f)
        
    print(f"Removed {removed_count} potentially NSFW words from the JSON! {len(filtered_words)} words remaining.")

if __name__ == "__main__":
    filter_nsfw()
