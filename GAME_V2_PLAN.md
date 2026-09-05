# First Light: V2 Game Loop Plan

## Overview
Shift the game from a linear, pre-generated sequence of words into a dynamic, endless "tug-of-war" survival mode. The game adapts to the player's performance in real-time, punishing mistakes by dropping the sun and shifting categories, while rewarding streaks with increasingly difficult thematic puzzles.

---

## 1. Core Mechanics

### The Tug-of-War Sun (Win/Loss Conditions)
*   **Correct Answer:** `score++`. The sun rises.
*   **Wrong Answer:** `score--`. The sun sinks back toward the horizon.
*   **Win Condition:** Score reaches `requiredScoreToWin` (e.g., 12). Sun fully clears the horizon.
*   **Loss Condition:** Score drops below `0` (e.g., to `-3`). The sun sinks completely, plunging the world into darkness (Game Over).

### Dynamic Difficulty
*   Instead of a pre-calculated list, the game maintains a `currentDifficulty` float (0.0 to 1.0).
*   **Correct Answer:** Difficulty increases (words get trickier, Deltas approach 0.00).
*   **Wrong Answer:** Difficulty decreases (words get easier, Deltas get larger).
*   The game fetches the *next* word dynamically based on the exact current difficulty.

### The Category Shift
*   Track `consecutiveWrongAnswers`.
*   If `consecutiveWrongAnswers == 2`, trigger a "Category Shift".
*   The game randomly selects two completely new categories (e.g., swapping from Dawn vs Hearth to Nocturne vs Canopy).
*   The player is thrown into a new thematic landscape, forcing them to quickly adapt their mental framework.

---

## 2. Technical Implementation

### Refactoring `DynamicRoundGenerator.cs`
*   **Current State:** Generates a massive array of words all at once during `Start()`.
*   **V2 State:** Becomes a persistent, stateful "Game Director".
    *   Loads the 10k JSON into memory once.
    *   Exposes a method: `GetNextWord(string catA, string catB, float difficulty, HashSet<string> usedWords)`.
    *   Calculates target Delta on the fly based on the `difficulty` parameter and returns a single word.

### Refactoring `Wordchooser.cs`
*   Removes the `rounds[]` array logic.
*   Maintains the `currentDifficulty`, `score`, and `consecutiveWrongAnswers` variables.
*   In `ResolveChoice()`:
    *   Update `score` up or down.
    *   Update `sunriseLightingController.Initialize()` dynamically to raise/lower the sun.
    *   Check Win/Loss conditions.
    *   Ask `DynamicRoundGenerator` for the next word.

---

## 3. UI Additions

*   **Difficulty Meter:** A vertical slider or UI graphic on the side of the screen that fills up as difficulty increases.
*   **Category Shift Warning:** A bold text flash in the center of the screen when categories are forced to swap.
*   **High Score Tracker:** A small UI element tracking the "Highest Difficulty Reached" for the current run, saved to `PlayerPrefs` so players have a reason to replay.
