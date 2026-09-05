# Agent Instructions: Semantic Game Loop & Round Data Generator

## Objective
Generate balanced, semantically scored round data for the 2.5D word-choice game and export it to a JSON format consumable by Unity's `SimpleWordChooser` component.

---

## 1. Core Semantic Categories
All target words are scored against these 5 primary category vectors:
* **Dawn:** light, sunrise, awakening, sky, morning
* **Nocturne:** shadow, void, sleep, mystery, midnight
* **Hearth:** fire, warmth, home, civilization, embers
* **Frost:** cold, ice, stillness, mist, winter
* **Canopy:** flora, roots, earth, growth, beasts

---

## 2. Mathematical Balancing Heuristics
For each candidate word, calculate cosine similarity $S_c$ against all 5 categories $c \in \{1 \dots 5\}$.

* **Impossible / Filtered Out:** If $\max(S_1 \dots S_5) < 0.25$, discard the word entirely.
* **Tricky Round (Duel):** Top two categories have high similarity ($S_1, S_2 > 0.35$) and are close together ($\vert{}S_1 - S_2\vert{} < 0.08$). Pair $C_1$ vs $C_2$.
* **Easy Round (Distinct):** Clear affinity to $C_{\text{top}}$ ($S_{\text{top}} > 0.40$), and lowest category $S_{\text{lowest}} < 0.20$. Pair $C_{\text{top}}$ vs $C_{\text{lowest}}$.

---

## 3. Environment & Execution Setup
Install dependencies using `uv`:

```bash
uv venv
source .venv/bin/activate
uv add sentence-transformers numpy
