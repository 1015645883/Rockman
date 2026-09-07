using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class BossDialogueManager : MonoBehaviour
{
    [Serializable]
    public class DialogueLine
    {
        public string speakerName;
        public string text;
        public Sprite speakerPortrait;
        public AudioClip voiceClip;
    }

    public GameObject dialogueUI;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public AudioSource voiceSource;
    public Image leftPortrait;     // 玩家头像 Image
    public Image rightPortrait;    // Boss头像 Image

    private DialogueLine[] dialogueLines;
    private int currentLine = 0;
    private Action onDialogueEnd;

    private bool isDialogueActive = false;
    private bool lineFullyDisplayed = false;

    void Start()
    {
        if (dialogueUI != null)
            dialogueUI.SetActive(false);
        if (leftPortrait != null)
            leftPortrait.gameObject.SetActive(false);
        if (rightPortrait != null)
            rightPortrait.gameObject.SetActive(false);
        if (speakerText != null)
            speakerText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isDialogueActive) return;

        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
        {
            if (lineFullyDisplayed)
            {
                PlayNextLine();
            }
            else
            {
                StopAllCoroutines();
                DialogueLine line = dialogueLines[currentLine];

                speakerText.text = line.speakerName;
                dialogueText.text = line.text;
                lineFullyDisplayed = true;
            }
        }
    }

    public void StartDialogue(DialogueLine[] lines, Action callback)
    {
        dialogueLines = lines;
        onDialogueEnd = callback;
        currentLine = -1;
        dialogueUI.SetActive(true);
        speakerText.gameObject.SetActive(true);
        isDialogueActive = true;

        if (leftPortrait != null) leftPortrait.gameObject.SetActive(false);
        if (rightPortrait != null) rightPortrait.gameObject.SetActive(false);

        PlayNextLine();
    }

    void PlayNextLine()
    {
        if (voiceSource.isPlaying)
            voiceSource.Stop();

        currentLine++;
        if (currentLine >= dialogueLines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueLines[currentLine];

        ShowSpeakerPortrait(line);

        if (line.voiceClip != null)
        {
            voiceSource.clip = line.voiceClip;
            voiceSource.Play();
        }

        StartCoroutine(TypeLine(line.speakerName, line.text));
    }

    IEnumerator TypeLine(string speakerName, string dialogueContent)
    {
        lineFullyDisplayed = false;
        speakerText.text = speakerName;
        dialogueText.text = "";

        foreach (char c in dialogueContent)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.05f);
        }

        lineFullyDisplayed = true;
    }

    void EndDialogue()
    {
        dialogueUI.SetActive(false);
        speakerText.gameObject.SetActive(false);
        isDialogueActive = false;

        if (leftPortrait != null) leftPortrait.gameObject.SetActive(false);
        if (rightPortrait != null) rightPortrait.gameObject.SetActive(false);

        onDialogueEnd?.Invoke();
    }

    void ShowSpeakerPortrait(DialogueLine line)
    {
        if (line.speakerName == "洛克人")
        {
            if (line.speakerPortrait != null)
            {
                leftPortrait.sprite = line.speakerPortrait;
                leftPortrait.gameObject.SetActive(true);
            }
            else
            {
                leftPortrait.gameObject.SetActive(false);
            }

            if (rightPortrait != null)
                rightPortrait.gameObject.SetActive(false);
        }
        else
        {
            if (line.speakerPortrait != null)
            {
                rightPortrait.sprite = line.speakerPortrait;
                rightPortrait.gameObject.SetActive(true);
            }
            else
            {
                rightPortrait.gameObject.SetActive(false);
            }

            if (leftPortrait != null)
                leftPortrait.gameObject.SetActive(false);
        }
    }
}
