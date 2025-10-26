using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro; // <-- ADD THIS LINE

// --- Enums to define Skill behaviors ---
[System.Serializable]
public enum SkillType
{
    Attack // <-- REMOVED "Heal"
}

[System.Serializable]
public enum TargetType
{
    SingleEnemy, // Hits the enemy
    AllEnemies,  // (Same as SingleEnemy in 1v1)
    SingleAlly,  // Hits the player (self)
    AllAllies,   // (Same as SingleAlly in 1v1)
    Self         // (Same as SingleAlly in 1v1)
}

// --- Skill Class ---
/// <summary>
/// Defines a single skill.
/// </summary>
[System.Serializable]
public class Skill
{
    public string skillName = "New Skill";
    public int power = 10; // Damage or healing amount
    public int manaCost = 0;
    public SkillType skillType = SkillType.Attack;
    public TargetType targetType = TargetType.SingleEnemy;
}

// --- Character Class ---
/// <summary>
/// Holds all data for a single character (player or enemy).
/// </summary>
[System.Serializable]
public class Character
{
    public string characterName = "New Character";
    public int maxHealth = 100;
    public int currentHealth;
    public int maxMana = 50;
    public int currentMana;
    public List<Skill> skills;

    public bool isAlive => currentHealth > 0;

    // "Copy Constructor"
    public Character(Character original)
    {
        this.characterName = original.characterName;
        this.maxHealth = original.maxHealth;
        this.currentHealth = original.maxHealth;
        this.maxMana = original.maxMana;
        this.currentMana = original.maxMana;
        this.skills = original.skills;
    }
}


/// <summary>
/// Manages the logic for a 1v1 turn-based 2D game with a flexible skill system.
/// </summary>
public class GameManager : MonoBehaviour
{
    // --- Character Setups ---
    // CONFIGURE YOUR TWO FIGHTERS HERE IN THE INSPECTOR!
    public Character playerSetup;
    public Skill playerBasicAttack; // <-- ADDED: Configure this as a "Skill" in inspector (power 10, mana 0)
    public Character enemySetup;
    public Skill enemyBasicAttack; // <-- ADDED: Configure this as a "Skill" in inspector (power 10, mana 0)

    // --- Private Combat Instances ---
    private Character player;
    private Character enemy;

    // --- ADD THESE LINES for Visuals ---
    public GameObject playerSpriteObject; // Drag your Player Sprite GameObject here
    public GameObject enemySpriteObject;  // Drag your Enemy Sprite GameObject here
    
    // --- ADD THESE LINES for Animation ---
    private Animator playerAnimator;
    private Animator enemyAnimator;
    private const string ATTACK_TRIGGER = "Attack";
    private const string SKILL_TRIGGER = "Skill"; // <-- ADDED
    private const string HIT_TRIGGER = "Hit";
    private const string DEFEATED_TRIGGER = "Defeated";
    // -------------------------------------

    // --- UI References ---
    public TextMeshProUGUI statusLogText; // <-- CHANGED
    public TextMeshProUGUI playerHealthText; // <-- CHANGED
    public TextMeshProUGUI enemyHealthText;  // <-- CHANGED

    // Assign buttons for skills (e.g., 4 skill slots per character)
    public Button attackButton; // <-- ADDED: The dedicated "Basic Attack" button
    public List<Button> skillButtons;

    // --- Game State ---
    private enum GameState
    {
        PlayerTurn,
        EnemyTurn,
        Won,
        Lost
    }
    private GameState currentState;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    void Start()
    {
        if (playerSetup == null || enemySetup == null || attackButton == null || playerBasicAttack == null || enemyBasicAttack == null)
        {
            Debug.LogError("Please set up Player, Enemy, Attack Button, and Basic Attacks in the Inspector!");
            return;
        }

        // --- ADDED: Add listener for the new attack button ---
        attackButton.onClick.AddListener(OnAttackButtonSelected);

        // --- ADDED: Get Animator components ---
        if (playerSpriteObject != null)
        {
            playerAnimator = playerSpriteObject.GetComponent<Animator>();
        }
        if (enemySpriteObject != null)
        {
            enemyAnimator = enemySpriteObject.GetComponent<Animator>();

            // --- ADD THIS to mirror the enemy sprite ---
            Vector3 enemyScale = enemySpriteObject.transform.localScale;
            enemyScale.x *= -1; // Flip the X scale
            enemySpriteObject.transform.localScale = enemyScale;
            // ---------------------------------------------
        }
        // ------------------------------------
        
        StartGame();
    }

    /// <summary>
    /// Sets up the initial game state.
    /// </summary>
    void StartGame()
    {
        // Create "instances" of the characters for this battle
        player = new Character(playerSetup);
        enemy = new Character(enemySetup);

        LogMessage($"{player.characterName} faces {enemy.characterName}!");
        
        // Wait a moment before starting the first turn
        StartCoroutine(StartFirstTurn());
    }

    private IEnumerator StartFirstTurn()
    {
        yield return new WaitForSeconds(1.5f);
        SetupPlayerTurn();
    }

    /// <summary>
    /// Configures the game for the player's turn.
    /// </summary>
    void SetupPlayerTurn()
    {
        currentState = GameState.PlayerTurn;
        LogMessage("Your turn. Choose an action!");
        UpdateUI();
        DisplaySkills();
        attackButton.interactable = true; // <-- ADDED: Always enable the basic attack button
    }

    /// <summary>
    /// Updates the skill buttons to show the active character's skills.
    /// </summary>
    void DisplaySkills()
    {
        // Clear all old listeners first to prevent bugs
        foreach (var button in skillButtons)
        {
            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(false);
        }

        // Set up buttons for each skill the character has
        for (int i = 0; i < player.skills.Count; i++)
        {
            if (i < skillButtons.Count) // Ensure we don't go out of bounds
            {
                Skill currentSkill = player.skills[i]; // Must capture in a local var for the listener

                skillButtons[i].gameObject.SetActive(true);
                skillButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = currentSkill.skillName; // <-- CHANGED

                // Check if player has enough mana
                // bool hasEnoughMana = player.currentMana >= currentSkill.manaCost; // <-- OLD LINE
                bool hasEnoughMana = true; // <-- NEW LINE: Player skills (Knight) are always available
                skillButtons[i].interactable = hasEnoughMana;

                // Add the listener
                if (hasEnoughMana)
                {
                    // When clicked, call OnSkillSelected with this specific skill
                    skillButtons[i].onClick.AddListener(() => OnSkillSelected(currentSkill));
                }
            }
        }
    }
    
    /// <summary>
    /// Sets the interactable state of all skill buttons.
    /// </summary>
    void SetSkillButtons(bool interactable)
    {
        foreach (var button in skillButtons)
        {
            button.interactable = interactable;
        }
    }

    // --- ADDED: New function for when the "Attack" button is clicked ---
    /// <summary>
    /// Called when the player clicks the dedicated "Attack" button.
    /// </summary>
    void OnAttackButtonSelected()
    {
        if (currentState != GameState.PlayerTurn) return; // Not player's turn

        // Disable all buttons
        SetSkillButtons(false);
        attackButton.interactable = false;

        // Start the action sequence with the predefined basic attack
        StartCoroutine(PlayerActionSequence(playerBasicAttack));
    }


    /// <summary>
    // Called when the player clicks a "Skill" button.
    /// </summary>
    void OnSkillSelected(Skill skill)
    {
        if (currentState != GameState.PlayerTurn) return; // Not player's turn

        // Disable all buttons to prevent multiple actions
        SetSkillButtons(false);
        attackButton.interactable = false; // <-- ADDED: Also disable the attack button

        // Start the action sequence
        StartCoroutine(PlayerActionSequence(skill));
    }

    /// <summary>
    /// Coroutine for executing the player's chosen skill.
    /// </summary>
    private IEnumerator PlayerActionSequence(Skill skill)
    {
        // 1. Pay Mana Cost
        // player.currentMana -= skill.manaCost; // <-- REMOVED: Knight skills don't cost mana
        // if (player.currentMana < 0) player.currentMana = 0; // <-- REMOVED

        string message = "";

        // 2. Apply Skill Effect
        switch (skill.targetType)
        {
            // --- ATTACKING THE ENEMY ---
            case TargetType.SingleEnemy:
            case TargetType.AllEnemies: // In 1v1, this is the same
                
                // --- TRIGGER ANIMATIONS ---
                if (skill == playerBasicAttack)
                {
                    if (playerAnimator != null) playerAnimator.SetTrigger(ATTACK_TRIGGER);
                }
                else
                {
                    // It's a special skill!
                    if (playerAnimator != null) playerAnimator.SetTrigger(SKILL_TRIGGER);
                }
                yield return new WaitForSeconds(0.5f); // Wait for attack anim to start
                if (enemyAnimator != null) enemyAnimator.SetTrigger(HIT_TRIGGER);
                // --------------------------

                enemy.currentHealth -= skill.power;
                if (enemy.currentHealth < 0) enemy.currentHealth = 0;
                message = $"{player.characterName} uses {skill.skillName} on {enemy.characterName} for {skill.power} damage!";
                break;

            // --- HEALING/BUFFING SELF (REMOVED) ---
        }

        LogMessage(message);
        UpdateUI();
        // yield return new WaitForSeconds(1.5f); // <-- We can shorten this, as the anims create a pause
        yield return new WaitForSeconds(1.0f); // <-- Shortened wait

        // 3. Check for win condition
        if (!enemy.isAlive)
        {
            EndGame(true); // Player won
        }
        else
        {
            // 4. Move to the enemy's turn
            StartCoroutine(EnemyTurnSequence());
        }
    }


    /// <summary>
    /// Coroutine for the enemy's turn logic.
    /// </summary>
    private IEnumerator EnemyTurnSequence()
    {
        currentState = GameState.EnemyTurn;
        LogMessage("Enemy's turn...");
        yield return new WaitForSeconds(1.5f);

        // --- UPDATED AI LOGIC ---
        Skill skillToUse;
        List<Skill> usableSkills = enemy.skills.Where(s => s.manaCost <= enemy.currentMana).ToList();
        
        // If no usable skills OR 50/50 chance, use basic attack
        if (usableSkills.Count == 0 || Random.Range(0, 2) == 0) 
        {
            skillToUse = enemyBasicAttack;
            LogMessage($"{enemy.characterName} prepares a basic attack.");
        }
        else
        {
            // Otherwise, pick a random special skill
            skillToUse = usableSkills[Random.Range(0, usableSkills.Count)];
        }
        // --- END OF UPDATED AI LOGIC ---

        if (skillToUse == null) // Failsafe
        {
             LogMessage($"{enemy.characterName} has no usable skills and does nothing.");
             yield return new WaitForSeconds(1.0f);
        }
        else
        {
            enemy.currentMana -= skillToUse.manaCost; // Pay mana cost (will be 0 for basic attack)

            string aiMessage = "";
            switch (skillToUse.targetType)
            {
                // --- ATTACKING THE PLAYER ---
                case TargetType.SingleEnemy:
                case TargetType.AllEnemies:
                    
                    // --- TRIGGER ANIMATIONS ---
                    if (skillToUse == enemyBasicAttack)
                    {
                        if (enemyAnimator != null) enemyAnimator.SetTrigger(ATTACK_TRIGGER);
                    }
                    else
                    {
                        // It's a special skill!
                        if (enemyAnimator != null) enemyAnimator.SetTrigger(SKILL_TRIGGER);
                    }
                    yield return new WaitForSeconds(0.5f); // Wait for attack anim to start
                    if (playerAnimator != null) playerAnimator.SetTrigger(HIT_TRIGGER);
                    // --------------------------

                    player.currentHealth -= skillToUse.power;
                        if (player.currentHealth < 0) player.currentHealth = 0;
                        aiMessage = $"{enemy.characterName} uses {skillToUse.skillName} on {player.characterName} for {skillToUse.power} damage!";
                    break;
                
                // --- HEALING/BUFFING SELF (REMOVED) ---
            }

            LogMessage(aiMessage);
            UpdateUI();
            // yield return new WaitForSeconds(1.5f); // <-- Shortened wait
            yield return new WaitForSeconds(1.0f);
        }

        // 3. Check for loss condition
        if (!player.isAlive)
        {
            EndGame(false); // Player lost
        }
        else
        {
            // 4. Move back to player's turn
            SetupPlayerTurn();
        }
    }

    /// <summary>
    /// Ends the game, displaying a win or loss message.
    /// </summary>
    void EndGame(bool playerWon)
    {
        SetSkillButtons(false); // Disable all skill buttons
        attackButton.interactable = false; // <-- ADDED: Also disable the attack button

        if (playerWon)
        {
            currentState = GameState.Won;
            LogMessage("You are victorious!");
            
            // --- UPDATED: Use animation trigger instead of SetActive ---
            if (enemyAnimator != null)
            {
                enemyAnimator.SetTrigger(DEFEATED_TRIGGER);
            }
            // ---------------------------------------------------------
        }
        else
        {
            currentState = GameState.Lost;
            LogMessage("You have been defeated...");

            // --- UPDATED: Use animation trigger instead of SetActive ---
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger(DEFEATED_TRIGGER);
            }
            // ----------------------------------------------------------
        }
    }

    /// <summary>
    /// Updates all health/mana UI text elements.
    /// </summary>
    void UpdateUI()
    {
        if (playerHealthText != null)
        {
            playerHealthText.text = $"{player.characterName} (Player)\nHP: {player.currentHealth} / {player.maxHealth}\nMP: {player.currentMana} / {player.maxMana}";
        }

        if (enemyHealthText != null)
        {
            enemyHealthText.text = $"{enemy.characterName} (Enemy)\nHP: {enemy.currentHealth} / {enemy.maxHealth}\nMP: {enemy.currentMana} / {enemy.maxMana}";
        }
    }

    /// <summary>
    /// Logs a message to the status text UI.
    /// </summary>
    void LogMessage(string message)
    {
        if (statusLogText != null)
        {
            statusLogText.text = message;
            Debug.Log(message);
        }
    }
}









