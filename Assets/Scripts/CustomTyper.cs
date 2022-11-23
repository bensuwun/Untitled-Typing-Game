using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class CustomTyper : MonoBehaviour
{
    private static int LEVEL;
    // Current word output
    public TextMeshProUGUI wordOutput;
    public TextMeshProUGUI wordOutput2;
    public TextMeshProUGUI wordOutput3;

    // Current WPM
    public TextMeshProUGUI currWPMText;

    public TextMeshProUGUI comboCounterText;
    public TextMeshProUGUI comboText;

    public WordBank wordBank = null;
    public StatsCalc statsCalc = null;
    public Player player = null;
    public Boss boss = null;
    public WordAnimator wordAnimator = null;
    private PlayerInventory inventory = null;
    private string sourceString = string.Empty;
    private string sourceString2 = string.Empty;
    private string sourceString3 = string.Empty; 
    private List<Word> wordList = new List<Word>();
    private List<Word> wordList2 = new List<Word>();
    private List<Word> wordList3 = new List<Word>();
    private StringBuilder sb;

    private double currWPM = 0d;
    private double wpmThreshold = 0d;

    // Indexing current word and current char
    private int wordIndex = 0;
    private int charIndex = 0;  // also represents number of typed characters
    private int caretPosition = 0;

    // Colors for correct, incorrect, default characters
    public string correctColor = "green";
    public string incorrectColor = "red";
    public string defaultColor = "#808080";

    private int numCharsTyped = 0;
    private int numCorrectChars = 0;
    private int numSpace = 0;

    private float lastIdleTime = 0f;
    private float idleTimeLimit = 5f;

    private int comboCount = 0;


    // Enums
    private enum Enums {
        // Input Formats
        IncorrectInputFormat = 0,
        CorrectInputFormat = 1,
        BackspaceInputFormat = 2,   // For resetting to default color when backspace is pressed
    }

    // Start is called before the first frame update
    void Start() {
        instantiateBattle(1);
    }

    public void instantiateBattle(int level) {
        inventory = (PlayerInventory) ScriptableObject.CreateInstance(className: "PlayerInventory");
        LEVEL = level;
        float bossHP = 0f;
        float playerHP = 100f;

        switch(LEVEL) {
            case 1:
                bossHP = 100f;
                wpmThreshold = 10d;
                idleTimeLimit = 10f;
                break;
            case 2:
                bossHP = 200f;
                wpmThreshold = 20d;
                idleTimeLimit = 6f;
                break;
            case 3:
                bossHP = 300f;
                wpmThreshold = 30d;
                idleTimeLimit = 3f;
                break;
            default:
                break;
        }
        // sets the boss's max HP
        boss.setMaxHP(bossHP);
        // sets the player's max HP 
        player.setMaxHP(playerHP);

        // sets the words that are displayed
        InitializeWordLists();

        // gets the current time
        statsCalc.getStart();

        // show current WPM to 0
        currWPMText.text = "" + currWPM;
        comboCounterText.text = "";
        comboText.text = "";
    }

    private void InitializeWordLists(){
        // For initialization: Each list gets filled with words
        
        if(wordList.Count == 0){
            SetWordListWords(wordList, out sourceString);
            SetTextGUI(wordOutput, sourceString);
        }
        if(wordList2.Count == 0){
            SetWordListWords(wordList2, out sourceString2);
            SetTextGUI(wordOutput2, sourceString2);
        }
        if(wordList3.Count == 0){
            SetWordListWords(wordList3, out sourceString3);
            SetTextGUI(wordOutput3, sourceString3);         // Display source string in output text
        }

        sb = new StringBuilder(sourceString); // display string for first line ; basis for many code below
    }

    private void SetCurrentWords(){
        // Once wordlist 1 is empty (Player is done with the line)
        // wordlist 1 <- wordlist 2 <- wordlist 3
        // then wordlist 3 gets new set of words
        wordList = wordList2;
        wordList2 = wordList3;
        wordList3 = new List<Word>();

        // Update words to display
        sourceString = String.Copy(sourceString2);
        sourceString2 = String.Copy(sourceString3);
        // sourceString3 = string.Empty;

        SetWordListWords(wordList3, out sourceString3);

        // Push updated words to GUI
        wordAnimator.wordNextLine(0);
        SetTextGUI(wordOutput, sourceString);
        wordAnimator.wordNextLine(1);
        SetTextGUI(wordOutput2, sourceString2);
        wordAnimator.wordNextLine(2);
        SetTextGUI(wordOutput3, sourceString3);


        sb = new StringBuilder(sourceString); 
    }
    public List<Word> getWordList(int number){
        if(number == 1)
            return wordList;
        else if(number == 2)
            return wordList2;
        else if(number == 3)
            return wordList3; 
        else return null;
    }

    public List<TMP_Text> GetTMPText_Components() {
        List<TMP_Text> textComponents = new List<TMP_Text>();
        textComponents.AddRange(new List<TMP_Text>
        {
            wordOutput.GetComponent<TMP_Text>(),
            //wordOutput2.GetComponent<TMP_Text>(),
            //wordOutput3.GetComponent<TMP_Text>()
        });

        return textComponents;
    }

    public void SetWordListWords(List<Word> words ,int number){
        string output = null;
        if(number == 1)
            wordList = words;
        else if(number == 2)
            wordList2 = words;
        else if(number == 3)
            wordList3 = words;

        foreach (var word in words){
            output += word.Text + " ";
            // Debug.Log(String.Format("Word : {0}", word.Text ));
        }
        output.Remove(output.LastIndexOf(" "));
        if(number == 1){
            SetTextGUI(wordOutput, output);
            sourceString = output;
        }else if(number == 2){
            SetTextGUI(wordOutput2, output);
            sourceString2 = output;
        }else if(number == 3){
            SetTextGUI(wordOutput3, output);
            sourceString3 = output;
        }
        Debug.Log(String.Format("Modified String: {0} Len: {1}",output, words.Count));
    }

    // Parse source string to list of words
    private void SetWordListWords(List<Word> words, out string outputStr){
        string stringFromBank = wordBank.GetWords();
        foreach(string str in stringFromBank.Split(" ")){
            Word newWord = new Word(str);
            words.Add(newWord);
        }
        outputStr = stringFromBank;
        // Debug.Log(String.Format("Output String: {0} Len: {1}",outputStr, words.Count));
        // Debug.Log(String.Format("Last Word: {0}", words[words.Count-1].IsFullyTyped()));
    }

    private void SetTextGUI(TextMeshProUGUI textArea, string str){
        textArea.SetText(str);
    }

    private void ResetIndeces(){
        caretPosition = 0;
        // wordIndex += 1; // temporary solution for extra " " at end of each set of words
        wordIndex = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // print(boss.isBossDead());
        // print(player.isPlayerDead());
        if(boss.isBossDead()) {
            print("THE BOSS IS DEAD");
            print("WPM: " + currWPM);
            SceneManager.LoadScene("Main Menu");
        }
        else if (player.isPlayerDead()) {
            print("YOU DIED");
            SceneManager.LoadScene("Main Menu");
        }
        else {
            string inputString = Input.inputString;
            
            if (inputString.Length == 1) {
                // wordAnimators[0].SetTrigger("NextLineTrigger");
                lastIdleTime = Time.time;
                switch (CheckInput(inputString[0])) {
                    // Character - can further be correct, incorrect, or excess
                    case 0:
                        EnterChar(inputString);
                        break;
                    // Spacebar
                    case 1:
                        EnterSpacebar();
                        break;
                    
                    // Backspace
                    case 2:
                        EnterBackspace();
                        break;
                    default:
                        break;

                }

                // Check if the current words on the screen are already finished and set new words
                if (AreWordsComplete()) {
                    SetCurrentWords();
                    ResetIndeces();
                }

                // Display current WPM on screen
                StartCoroutine(checkWPM());
                StartCoroutine(checkIdle());
                StartCoroutine(updateCombo());
                StartCoroutine(obtainDebuff());
            } 
        }
    }

    /**
        Returns the type of the given input
        0 - character
        1 - spacebar
        2 - backspace
    */
    int CheckInput(char input) {
        int type = 0;

        // spacebar
        if (input == ' ')
            type = 1;

        // backspace
        else if (input == '\b')
            type = 2;
    
        return type;
    }

    /**
        Returns colored character in rich text format
    */
    string FormatInput(string input, int mode) {
        if (mode == (int) Enums.CorrectInputFormat) {
            return String.Format("<color={0}>{1}</color>", correctColor, input);
        }
        else if (mode == (int) Enums.IncorrectInputFormat) {
            return String.Format("<color={0}>{1}</color>", incorrectColor, input);
        }
        else 
            return String.Format("<color={0}>{1}</color>", defaultColor, input);
    }

    /**
        Entered a character (A-Z).
        Checks if input is correct, incorrect, or an excess.
    */
    void EnterChar(string input) {
        Debug.Log(string.Format("Word Index: {0} | Char Index: {1} | Caret Position: {2}", 
            wordIndex, charIndex, caretPosition));
            
        // If current word input is already longer than actual word, show as incorrect
        if (wordList[wordIndex].IsFullyTyped()) {
            string formattedInput = FormatInput(input, (int) Enums.IncorrectInputFormat);
            sb.Insert(caretPosition, formattedInput);

            // Update word output
            wordOutput.text = sb.ToString();

            // Update caret position
            caretPosition += formattedInput.Length;

            // Update charIndex
            charIndex += 1;
            player.TakeDamage(1);
            comboCount = 0;
        }

        // Check if input char is correct
        else if (input[0] == wordList[wordIndex].GetNextChar()) {
            // Replace character with green character
            string formattedInput = FormatInput(input, (int) Enums.CorrectInputFormat);
            sb.Remove(caretPosition, 1).Insert(caretPosition, formattedInput);

            // Update word output
            wordOutput.text = sb.ToString();

            // Update caret position
            caretPosition += formattedInput.Length;

            // Update char index and word properties
            charIndex += 1;
            wordList[wordIndex].nTyped += 1;
            wordList[wordIndex].nCorrect += 1;
            numCorrectChars += 1;

            boss.TakeDamage(.5f, wpmThreshold, currWPM, comboCount);

            comboCount += 1;
        }

        // Incorrect
        else if (input[0] != wordList[wordIndex].GetNextChar()) {
            string origChar = wordList[wordIndex].Text[charIndex].ToString();
            string formattedInput = FormatInput(origChar, (int) Enums.IncorrectInputFormat);
            sb.Remove(caretPosition, 1).Insert(caretPosition, formattedInput);

            // Update word output
            wordOutput.text = sb.ToString();

            // Update caret position
            caretPosition += formattedInput.Length;

            // Update char index and word properties
            charIndex += 1;
            wordList[wordIndex].nTyped += 1;
            player.TakeDamage(1);
            comboCount = 0;
        }

        numCharsTyped += 1;
    }

    /**
        Whitespace - end of current word
        Rich Text Tag - current word (check for excess)
    */
    void EnterBackspace() {
        int nTag = 0;
        bool onCurrentWord = false;
        
        char? lastChar = null;
        // Backspacing current word, check for excess
        if (charIndex > 0) {
            onCurrentWord = true;
            
            if (charIndex <= wordList[wordIndex].Text.Length)
                lastChar = wordList[wordIndex].GetPrevChar();
        }

        if (onCurrentWord) {
            // Current Implementation: Mid-Word
            while (nTag != 2) {
                caretPosition -= 1;
                if (sb[caretPosition] == '<') {
                    nTag++;
                }
                sb.Remove(caretPosition, 1);
            }
            
            // Not excess
            if (lastChar != null) {
                sb.Insert(caretPosition, lastChar);
                wordList[wordIndex].nTyped -= 1;
            }
            
            // Update word output
            wordOutput.text = sb.ToString();

            // Update word properties
            charIndex -= 1;  
        }

        comboCount = 0;
    }

    /**
        On spacebar input, checks for premature presses.
    */
    void EnterSpacebar() {
  
        // do nothing if no character typed
        if(charIndex == 0){
            return;
        }
        // Check for premature spacebar (word has not finished yet)
        if (!wordList[wordIndex].IsFullyTyped()) {
            comboCount = 0;
            caretPosition += wordList[wordIndex].GetRemainingChars() + 1; /// + 1 for whitespace
        }
        // Move caret only one (user finished the word)
        else {
            caretPosition += 1;
            numSpace += 1;
        }
        // Next word, update indices
        wordIndex += 1;
        charIndex = 0;
        
      
    }

    // Checks if the current line is already finished, return true if done, false if not
    private bool AreWordsComplete() {
        // check length of the remaining line
        // no next word
        bool isThereNoNextWord = wordList.Count == (wordIndex + 1);
        bool isWordFullyTyped = wordList[wordIndex].IsFullyTyped();
        return isThereNoNextWord && isWordFullyTyped;
    }

    public PlayerInventory GetInventory(){
        return inventory;
    }

    // sets the WPM
    private IEnumerator checkWPM() {
        while (true) {
            currWPM = statsCalc.getCurrWPM(numCorrectChars, numSpace);
            currWPMText.text = "" + currWPM;
            yield return null;
        }
        
    }

    // checks for idleness and makes the player take damage whenever idle
    private IEnumerator checkIdle() {
        while (true) {
            if (Time.time - lastIdleTime > idleTimeLimit) {
                player.TakeDamage(10);
                comboCount = 0;
                lastIdleTime = Time.time;
            }
        
            yield return null;
        }
        
    }

    private IEnumerator updateCombo() {
        string showComboText = "";
        string text = "";
        string textColor = "white";
        while (true) {
            if (comboCount!= 0) {
                showComboText = "Combo:";
                text = "" + comboCount;
            }
            else {
                showComboText = "";
                text = "";
            }

            if (comboCount > 0) textColor = "white";
            if (comboCount >= 10) textColor = "#00a0ff";
            if (comboCount >= 20) textColor = "#24e100";
            if (comboCount >= 30) textColor = "#ffe300";
            if (comboCount >= 40) textColor = "#ff7f1c";
            if (comboCount >= 50) textColor = "#ff3af2";

            if (comboCount % 35 == 0) inventory.clearDebuffFlag = true;
            if (comboCount % 50 == 0) inventory.hpRegenFlag = true;
            if (comboCount % 60 == 0) inventory.buttonMashFlag = true;

            string formattedText = String.Format("<color={0}>{1}</color>", textColor, text);
            string formattedComboText = String.Format("<color={0}>{1}</color>", textColor, showComboText);

            comboCounterText.text = formattedText;
            comboText.text = formattedComboText;
            
            float fontSize = 60f;
            float perHitFontSize = 0.7f;

            comboCounterText.fontSize = fontSize + perHitFontSize * comboCount;
            yield return null;
        }
    }

    private IEnumerator obtainDebuff(){
        yield return new WaitForSeconds(5);
        while(true){
            int rngNum = UnityEngine.Random.Range(1,4);
            if (rngNum == 1){
                inventory.shortSightedFlag = true;
                Debug.Log("Debuff Sight");
            }else if(rngNum == 2){
                inventory.armsSpaghettiFlag = true;
                Debug.Log("Debuff Spage");
            }else if(rngNum == 3){
                inventory.longWordsFlag = true;
                Debug.Log("Debuff Long ");
            }
            yield return new WaitForSeconds(5);
        }
    }
}
