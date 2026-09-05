import json
import requests

def filter_words():
    valid_english = set()
    with open('/usr/share/dict/words', 'r') as f:
        for line in f:
            w = line.strip()
            if w.islower():
                valid_english.add(w)

    url = "https://raw.githubusercontent.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words/master/en"
    resp = requests.get(url)
    bad_words = set([w.strip().lower() for w in resp.text.splitlines()])

    custom_banned = {
        "marijuana", "weed", "cannabis", "cocaine", "heroin", "meth", "lsd", "acid", "ecstasy", 
        "shrooms", "bong", "joint", "blunt", "hash", "opium", "fentanyl", "narcotic", "narcotics",
        "beer", "wine", "vodka", "whiskey", "rum", "tequila", "liquor", "booze", "drunk", "hangover",
        "cigarette", "cigar", "tobacco", "nicotine", "vape", "vaping", "suicide", "murder", "rape",
        "kill", "killer", "killing", "terrorist", "terrorism", "bomb", "bombing", "gun", "rifle",
        "weapon", "weapons", "blood", "bloody", "death", "dead", "deadly", "die", "dying", "corpse",
        "skull", "skeleton", "devil", "satan", "demon", "demonic", "hell", "hellish", "damn", "damned"
    }
    
    with open("Assets/word_database.json", "r") as f:
        data = json.load(f)
        
    original_count = len(data["words"])
    
    filtered_words = []
    for w in data["words"]:
        word = w["word"].lower()
        
        if word not in valid_english:
            continue
        if word in bad_words:
            continue
        if word in custom_banned:
            continue
            
        filtered_words.append(w)
            
    removed_count = original_count - len(filtered_words)
    
    data["words"] = filtered_words
    
    with open("Assets/word_database.json", "w") as f:
        json.dump(data, f)
        
    print(f"Removed {removed_count} names, places, and questionable words! {len(filtered_words)} words remaining.")

if __name__ == "__main__":
    filter_words()
