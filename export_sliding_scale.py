import sqlite3
import json
import itertools

def get_sliding_scale(c, cat_a, cat_b):
    # Filter 1: The word must fundamentally belong to this pair. 
    # (i.e. its highest score across ALL 5 categories must be A or B)
    # Filter 2: It cannot be a poor match for both.
    # (i.e. Score A + Score B > 0.70)
    
    query = f"""
    SELECT word, {cat_a}, {cat_b}, ({cat_a} - {cat_b}) as delta
    FROM scores
    WHERE ({cat_a} = MAX(dawn, nocturne, hearth, frost, canopy) 
       OR {cat_b} = MAX(dawn, nocturne, hearth, frost, canopy))
      AND ({cat_a} + {cat_b}) > 0.70
    ORDER BY delta DESC
    """
    c.execute(query)
    results = c.fetchall()
    
    words_data = []
    for row in results:
        words_data.append({
            "word": row[0],
            f"{cat_a}_score": round(row[1], 3),
            f"{cat_b}_score": round(row[2], 3),
            "delta": round(row[3], 3)
        })
        
    return words_data

def main():
    conn = sqlite3.connect('semantic_scores.db')
    c = conn.cursor()
    
    categories = ["dawn", "nocturne", "hearth", "frost", "canopy"]
    
    # We will generate the sliding scale for ALL 10 possible pairs, 
    # so the Unity game has total flexibility for any round combination!
    all_pairs = list(itertools.combinations(categories, 2))
    
    export_data = {}
    
    for pair in all_pairs:
        cat_a, cat_b = pair
        pair_name = f"{cat_a.capitalize()}_vs_{cat_b.capitalize()}"
        words_list = get_sliding_scale(c, cat_a, cat_b)
        export_data[pair_name] = words_list
        print(f"Generated {len(words_list)} valid words for {pair_name}")
        
    conn.close()
    
    # Export to JSON
    out_file = "sliding_scale_export.json"
    with open(out_file, "w") as f:
        json.dump(export_data, f, indent=2)
        
    print(f"\nDone! Exported full sliding scale dataset to {out_file}")

if __name__ == "__main__":
    main()
