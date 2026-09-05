import json
import math

def get_smooth_progression(all_data, pair_key, cat_a, cat_b, num_per_category=4):
    words = all_data[pair_key]
    
    # Split into A-words (delta > 0) and B-words (delta < 0)
    a_words = [w for w in words if w["delta"] > 0]
    b_words = [w for w in words if w["delta"] < 0]
    
    def pick_smooth_sequence(word_pool, cat_name, is_a):
        if not word_pool: return []
        
        # Sort pool so we can find max delta
        word_pool.sort(key=lambda x: abs(x["delta"]), reverse=True)
        max_delta = abs(word_pool[0]["delta"])
        
        # We want to step down from max_delta to near 0
        # e.g., if max_delta is 0.50, targets might be [0.50, 0.33, 0.16, 0.02]
        targets = []
        for i in range(num_per_category):
            # linear interpolation from max_delta to 0.02
            t = 1.0 - (i / float(num_per_category - 1)) if num_per_category > 1 else 1.0
            targets.append(max(0.02, max_delta * t))
            
        sequence = []
        used = set()
        
        for target_d in targets:
            # Find the 15 words closest to this target delta
            available = [w for w in word_pool if w["word"] not in used]
            if not available: break
                
            # Sort by how close their delta is to the target
            available.sort(key=lambda x: abs(abs(x["delta"]) - target_d))
            closest_candidates = available[:15]
            
            # Out of those closest in delta, pick the one with the HIGHEST primary score
            # This ensures the target category score remains high, but the delta shrinks 
            # (meaning the secondary category score is creeping up!)
            score_key = f"{cat_name.lower()}_score"
            best_word = max(closest_candidates, key=lambda x: x[score_key])
            
            sequence.append(best_word)
            used.add(best_word["word"])
            
        return sequence

    a_sequence = pick_smooth_sequence(a_words, cat_a, True)
    b_sequence = pick_smooth_sequence(b_words, cat_b, False)
    
    # Interleave them for gameplay (Easy A, Easy B, Medium A, Medium B, etc.)
    final_round = []
    for i in range(max(len(a_sequence), len(b_sequence))):
        if i < len(a_sequence):
            w = a_sequence[i]
            final_round.append({
                "word": w["word"],
                "leftCategory": cat_a,
                "rightCategory": cat_b,
                "isLeftCorrect": True,
                "_debug_delta": w["delta"],
                "_debug_score": w[f"{cat_a.lower()}_score"]
            })
        if i < len(b_sequence):
            w = b_sequence[i]
            final_round.append({
                "word": w["word"],
                "leftCategory": cat_a,
                "rightCategory": cat_b,
                "isLeftCorrect": False,
                "_debug_delta": w["delta"],
                "_debug_score": w[f"{cat_b.lower()}_score"]
            })
            
    return final_round

def main():
    with open("sliding_scale_export.json", "r") as f:
        data = json.load(f)
        
    final_rounds = []
    
    rounds_config = [
        ("Dawn_vs_Hearth", "Dawn", "Hearth"),
        ("Nocturne_vs_Canopy", "Nocturne", "Canopy"),
        ("Dawn_vs_Frost", "Dawn", "Frost") # Frost vs Dawn (key is Dawn_vs_Frost)
    ]
    
    for key, cat_a, cat_b in rounds_config:
        print(f"\nGenerating smooth progression for {cat_a} vs {cat_b}...")
        round_words = get_smooth_progression(data, key, cat_a, cat_b, num_per_category=4)
        
        for idx, w in enumerate(round_words):
            cat = cat_a if w["isLeftCorrect"] else cat_b
            print(f"  {idx+1}. {w['word']:>12} | Target: {cat:8} | Score: {w['_debug_score']:.2f} | Delta: {abs(w['_debug_delta']):.2f}")
            
        final_rounds.extend(round_words)
        
    # Clean up debug keys before export
    for w in final_rounds:
        del w["_debug_delta"]
        del w["_debug_score"]
        
    unity_data = { "rounds": final_rounds }
    with open("Assets/game_rounds.json", "w") as f:
        json.dump(unity_data, f, indent=2)
        
    print(f"\nExported {len(final_rounds)} smoothly progressing rounds to Assets/game_rounds.json!")

if __name__ == "__main__":
    main()
