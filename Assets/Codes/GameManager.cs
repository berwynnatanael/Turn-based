using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro; 

[System.Serializable]
public enum SkillType
{
    Attack 
}

[System.Serializable]
public enum TargetType
{
    SingleEnemy, 
    AllEnemies,  
    SingleAlly,  
    AllAllies,   
    Self         
}

[System.Serializable]
public class Skill
{
    public string skillName = "New Skill";
    public int power = 10; 
    public int manaCost = 0;
    public SkillType skillType = SkillType.Attack;
    public TargetType targetType = TargetType.SingleEnemy;
}

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


public class GameManager : MonoBehaviour
{
    public Character playerSetup;
    public Skill playerBasicAttack; 
    public Character enemySetup;
    public Skill enemyBasicAttack; 

    private Character player;
    private Character enemy;

    public GameObject playerSpriteObject;
    public GameObject enemySpriteObject;
    
    private Animator playerAnimator;
    private Animator enemyAnimator;
    private const string ATTACK_TRIGGER = "Attack";
    private const string SKILL_TRIGGER = "Skill"; 
    private const string HIT_TRIGGER = "Hit";
    private const string DEFEATED_TRIGGER = "Defeated";

    public TextMeshProUGUI statusLogText; 
    public TextMeshProUGUI playerHealthText;
    public TextMeshProUGUI enemyHealthText; 

    public Button attackButton; 
    public List<Button> skillButtons;

    private enum GameState
    {
        PlayerTurn,
        EnemyTurn,
        Won,
        Lost
    }
    private GameState currentState;

    void Start()
    {
        if (playerSetup == null || enemySetup == null || attackButton == null || playerBasicAttack == null || enemyBasicAttack == null)
        {
            Debug.LogError("set up in the inspector first");
            return;
        }

        attackButton.onClick.AddListener(OnAttackButtonSelected);

        if (playerSpriteObject != null)
        {
            playerAnimator = playerSpriteObject.GetComponent<Animator>();
        }
        if (enemySpriteObject != null)
        {
            enemyAnimator = enemySpriteObject.GetComponent<Animator>();

            Vector3 enemyScale = enemySpriteObject.transform.localScale;
            enemyScale.x *= -1; 
            enemySpriteObject.transform.localScale = enemyScale;
        }
        
        StartGame();
    }

    void StartGame()
    {
        player = new Character(playerSetup);
        enemy = new Character(enemySetup);

        LogMessage($"{player.characterName} faces {enemy.characterName}!");
        
        StartCoroutine(StartFirstTurn());
    }

    private IEnumerator StartFirstTurn()
    {
        yield return new WaitForSeconds(1.5f);
        SetupPlayerTurn();
    }

    void SetupPlayerTurn()
    {
        currentState = GameState.PlayerTurn;
        LogMessage("Your turn. Choose an action!");
        UpdateUI();
        DisplaySkills();
        attackButton.interactable = true; 
    }

    void DisplaySkills()
    {
        foreach (var button in skillButtons)
        {
            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(false);
        }

        for (int i = 0; i < player.skills.Count; i++)
        {
            if (i < skillButtons.Count) 
            {
                Skill currentSkill = player.skills[i];

                skillButtons[i].gameObject.SetActive(true);
                skillButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = currentSkill.skillName; 

                bool hasEnoughMana = true; 
                skillButtons[i].interactable = hasEnoughMana;

                if (hasEnoughMana)
                {
                    skillButtons[i].onClick.AddListener(() => OnSkillSelected(currentSkill));
                }
            }
        }
    }
 
    void SetSkillButtons(bool interactable)
    {
        foreach (var button in skillButtons)
        {
            button.interactable = interactable;
        }
    }

 
    void OnAttackButtonSelected()
    {
        if (currentState != GameState.PlayerTurn) return; 

        SetSkillButtons(false);
        attackButton.interactable = false;

        StartCoroutine(PlayerActionSequence(playerBasicAttack));
    }


    void OnSkillSelected(Skill skill)
    {
        if (currentState != GameState.PlayerTurn) return;

        SetSkillButtons(false);
        attackButton.interactable = false; 

        StartCoroutine(PlayerActionSequence(skill));
    }

    private IEnumerator PlayerActionSequence(Skill skill)
    {

        string message = "";

        switch (skill.targetType)
        {
            case TargetType.SingleEnemy:
            case TargetType.AllEnemies: 
                
                if (skill == playerBasicAttack)
                {
                    if (playerAnimator != null) playerAnimator.SetTrigger(ATTACK_TRIGGER);
                }
                else
                {
                    if (playerAnimator != null) playerAnimator.SetTrigger(SKILL_TRIGGER);
                }
                yield return new WaitForSeconds(0.5f); 
                if (enemyAnimator != null) enemyAnimator.SetTrigger(HIT_TRIGGER);

                enemy.currentHealth -= skill.power;
                if (enemy.currentHealth < 0) enemy.currentHealth = 0;
                message = $"{player.characterName} uses {skill.skillName} on {enemy.characterName} for {skill.power} damage!";
                break;

        }

        LogMessage(message);
        UpdateUI();
        yield return new WaitForSeconds(1.0f); 

        if (!enemy.isAlive)
        {
            EndGame(true); 
        }
        else
        {
            StartCoroutine(EnemyTurnSequence());
        }
    }


    private IEnumerator EnemyTurnSequence()
    {
        currentState = GameState.EnemyTurn;
        LogMessage("Enemy's turn...");
        yield return new WaitForSeconds(1.5f);

        Skill skillToUse;
        List<Skill> usableSkills = enemy.skills.Where(s => s.manaCost <= enemy.currentMana).ToList();
        
        if (usableSkills.Count == 0 || Random.Range(0, 2) == 0) 
        {
            skillToUse = enemyBasicAttack;
            LogMessage($"{enemy.characterName} prepares a basic attack.");
        }
        else
        {
            skillToUse = usableSkills[Random.Range(0, usableSkills.Count)];
        }

        if (skillToUse == null) 
        {
             LogMessage($"{enemy.characterName} has no usable skills and does nothing.");
             yield return new WaitForSeconds(1.0f);
        }
        else
        {
            enemy.currentMana -= skillToUse.manaCost; 

            string aiMessage = "";
            switch (skillToUse.targetType)
            {
                case TargetType.SingleEnemy:
                case TargetType.AllEnemies:
                    
                    if (skillToUse == enemyBasicAttack)
                    {
                        if (enemyAnimator != null) enemyAnimator.SetTrigger(ATTACK_TRIGGER);
                    }
                    else
                    {
                        if (enemyAnimator != null) enemyAnimator.SetTrigger(SKILL_TRIGGER);
                    }
                    yield return new WaitForSeconds(0.5f); 
                    if (playerAnimator != null) playerAnimator.SetTrigger(HIT_TRIGGER);
                    // --------------------------

                    player.currentHealth -= skillToUse.power;
                        if (player.currentHealth < 0) player.currentHealth = 0;
                        aiMessage = $"{enemy.characterName} uses {skillToUse.skillName} on {player.characterName} for {skillToUse.power} damage!";
                    break;
                
            }

            LogMessage(aiMessage);
            UpdateUI();
            yield return new WaitForSeconds(1.0f);
        }

        if (!player.isAlive)
        {
            EndGame(false); 
        }
        else
        {
            SetupPlayerTurn();
        }
    }

    void EndGame(bool playerWon)
    {
        SetSkillButtons(false); 
        attackButton.interactable = false; 

        if (playerWon)
        {
            currentState = GameState.Won;
            LogMessage("You are victorious!");
            
            if (enemyAnimator != null)
            {
                enemyAnimator.SetTrigger(DEFEATED_TRIGGER);
            }
        }
        else
        {
            currentState = GameState.Lost;
            LogMessage("You have been defeated...");

            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger(DEFEATED_TRIGGER);
            }
        }
    }

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

    void LogMessage(string message)
    {
        if (statusLogText != null)
        {
            statusLogText.text = message;
            Debug.Log(message);
        }
    }
}









