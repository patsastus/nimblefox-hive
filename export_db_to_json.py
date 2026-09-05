import sqlite3
import json

def export_db():
    conn = sqlite3.connect('semantic_scores.db')
    c = conn.cursor()
    
    c.execute("SELECT word, dawn, nocturne, hearth, frost, canopy FROM scores")
    rows = c.fetchall()
    
    words_data = []
    for row in rows:
        words_data.append({
            "word": row[0],
            "dawn": round(row[1], 3),
            "nocturne": round(row[2], 3),
            "hearth": round(row[3], 3),
            "frost": round(row[4], 3),
            "canopy": round(row[5], 3)
        })
        
    conn.close()
    
    # We wrap it in an object so Unity's JsonUtility can parse it as an array
    out_data = { "words": words_data }
    
    with open("Assets/word_database.json", "w") as f:
        json.dump(out_data, f)
        
    print(f"Exported {len(words_data)} words to Assets/word_database.json!")

if __name__ == "__main__":
    export_db()
