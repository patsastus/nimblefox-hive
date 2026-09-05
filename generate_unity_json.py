import json

def get_words_for_round(all_data, pair_key, cat_left, cat_right, num_easy=2, num_tricky=4):
    words = all_data[pair_key]
    # words is sorted by (scoreA - scoreB) descending
    # index 0 is strongest A, index -1 is strongest B
    
    rounds = []
    
    # 1. Easy A (Left)
    easy_left = words[0]
    rounds.append({
        "word": easy_left["word"],
        "leftCategory": cat_left,
        "rightCategory": cat_right,
        "isLeftCorrect": True
    })
    
    # 2. Easy B (Right)
    easy_right = words[-1]
    rounds.append({
        "word": easy_right["word"],
        "leftCategory": cat_left,
        "rightCategory": cat_right,
        "isLeftCorrect": False
    })
    
    # 3. Tricky (Middle)
    mid_idx = len(words) // 2
    tricky_start = mid_idx - (num_tricky // 2)
    tricky_words = words[tricky_start : tricky_start + num_tricky]
    
    for t in tricky_words:
        is_left_correct = t["delta"] > 0
        rounds.append({
            "word": t["word"],
            "leftCategory": cat_left,
            "rightCategory": cat_right,
            "isLeftCorrect": is_left_correct
        })
        
    return rounds

def main():
    with open("sliding_scale_export.json", "r") as f:
        data = json.load(f)
        
    final_rounds = []
    
    # 1. Dawn vs Hearth
    final_rounds.extend(get_words_for_round(data, "Dawn_vs_Hearth", "Dawn", "Hearth"))
    
    # 2. Nocturne vs Canopy
    final_rounds.extend(get_words_for_round(data, "Nocturne_vs_Canopy", "Nocturne", "Canopy"))
    
    # 3. Frost vs Dawn
    final_rounds.extend(get_words_for_round(data, "Dawn_vs_Frost", "Dawn", "Frost")) # Note: key is Dawn_vs_Frost in JSON
    
    # Export for Unity
    unity_data = { "rounds": final_rounds }
    with open("Assets/game_rounds.json", "w") as f:
        json.dump(unity_data, f, indent=2)
        
    print(f"Exported {len(final_rounds)} rounds to Assets/game_rounds.json!")

if __name__ == "__main__":
    main()
