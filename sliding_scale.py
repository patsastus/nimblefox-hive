import sqlite3

def get_sliding_scale(c, cat_a, cat_b, limit_each=5):
    # Filter: The word must primarily belong to A or B
    # Meaning A or B must be its highest score across all 5 categories
    query = f"""
    SELECT word, {cat_a}, {cat_b}, ({cat_a} - {cat_b}) as delta
    FROM scores
    WHERE ({cat_a} = MAX(dawn, nocturne, hearth, frost, canopy) 
       OR {cat_b} = MAX(dawn, nocturne, hearth, frost, canopy))
      AND MAX({cat_a}, {cat_b}) > 0.30
    ORDER BY delta DESC
    """
    c.execute(query)
    results = c.fetchall()
    
    # results is ordered from +1.0 (Easiest A) down to 0.0 (Tricky) down to -1.0 (Easiest B)
    if not results:
        return
        
    easiest_a = results[:limit_each]
    easiest_b = results[-limit_each:]
    easiest_b.reverse() # flip so strongest B is first
    
    # Find the trickiest (delta closest to 0)
    # Sort by absolute delta
    trickiest = sorted(results, key=lambda x: abs(x[3]))[:limit_each]
    
    print(f"\n=== SLIDING SCALE: {cat_a.capitalize()} vs {cat_b.capitalize()} ===")
    
    print(f"\n--- Easiest {cat_a.capitalize()} (Delta ~ {easiest_a[0][3]:.2f}) ---")
    for row in easiest_a:
        print(f"{row[0]:>12} | {cat_a}: {row[1]:.2f}, {cat_b}: {row[2]:.2f} (Delta: +{row[3]:.2f})")
        
    print("\n--- The Tricky Middle (Delta ~ 0.00) ---")
    for row in trickiest:
        # Just to show the sign
        sign = "+" if row[3] >= 0 else ""
        print(f"{row[0]:>12} | {cat_a}: {row[1]:.2f}, {cat_b}: {row[2]:.2f} (Delta: {sign}{row[3]:.2f})")
        
    print(f"\n--- Easiest {cat_b.capitalize()} (Delta ~ {easiest_b[0][3]:.2f}) ---")
    for row in easiest_b:
        print(f"{row[0]:>12} | {cat_a}: {row[1]:.2f}, {cat_b}: {row[2]:.2f} (Delta: {row[3]:.2f})")


def main():
    conn = sqlite3.connect('semantic_scores.db')
    c = conn.cursor()
    get_sliding_scale(c, "nocturne", "canopy", limit_each=5)
    conn.close()

if __name__ == "__main__":
    main()
