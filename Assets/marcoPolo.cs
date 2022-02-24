using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using rnd = UnityEngine.Random;

public class marcoPolo : MonoBehaviour
{
    public new KMAudio audio;
    public AudioSource beepPlayer;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable[] buttons;
    public KMSelectable soundButton;
    public TextMesh[] buttonTexts;
    public Renderer[] leds;
    public Color[] textColors;
    public Color on;
    public Color off;
    public Color solvedColor;
    public Color strikeColor;

    private int stage;
    private bool[] isBlue = new bool[3];
    private int[] directionIndices = new int[3];
    private int[] solution = new int[3];
    private int[][] labelOrders = new int[3][];

    private bool cantPress;
    private bool perfect = true;
    private static readonly string[] labels = new[] { "L", "M", "R" };
    private static readonly string[] directionNames = new[] { "left", "middle", "right" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { PressButton(button); return false; };
        soundButton.OnInteract += delegate () { StartCoroutine(PressSoundButton()); return false; };
    }

    private void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            isBlue[i] = rnd.Range(0, 2) == 0;
            directionIndices[i] = rnd.Range(0, 3);
            labelOrders[i] = Enumerable.Range(0, 3).ToList().Shuffle().ToArray();
            Debug.LogFormat("[Marco Polo #{0}] Stage {1}:", moduleId, i + 1);
            if (isBlue[i])
            {
                solution[i] = Array.IndexOf(labelOrders[i], directionIndices[i]);
                Debug.LogFormat("[Marco Polo #{0}] The text is blue, and the sound is coming from the {1}, so the correct button to press is the one with that label.", moduleId, directionNames[directionIndices[i]]);
            }
            else
            {
                solution[i] = directionIndices[i];
                Debug.LogFormat("[Marco Polo #{0}] The text is black, and the sound is coming from the {1}, so the correct button to press is the one in that position.", moduleId, directionNames[solution[i]]);
            }
        }
        StartCoroutine(UpdateButtons());
    }

    private IEnumerator UpdateButtons()
    {
        cantPress = true;
        if (stage != 0)
        {
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(.3f);
                audio.PlaySoundAtTransform("tap", buttons[i].transform);
                buttonTexts[i].text = "";
            }
            yield return new WaitForSeconds(.4f);
            for (int i = 0; i < 3; i++)
            {
                if (i != 0)
                    yield return new WaitForSeconds(.3f);
                audio.PlaySoundAtTransform("tap", buttons[i].transform);
                buttonTexts[i].text = labels[labelOrders[stage][i]];
                buttonTexts[i].color = !isBlue[stage] ? textColors[0] : textColors[1];
            }
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                buttonTexts[i].text = labels[labelOrders[stage][i]];
                buttonTexts[i].color = !isBlue[stage] ? textColors[0] : textColors[1];
            }
        }
        cantPress = false;
    }

    private IEnumerator PressSoundButton()
    {
        soundButton.AddInteractionPunch(.5f);
        if (cantPress)
            yield break;
        cantPress = true;
        if (beepPlayer.isPlaying)
            beepPlayer.Stop();
        if (moduleSolved)
            SetBeepPlayer(labels[rnd.Range(0, 3)]);
        else
            SetBeepPlayer(labels[directionIndices[stage]]);
        beepPlayer.Play();
        yield return new WaitForSeconds(.3f);
        cantPress = false;
    }

    private void SetBeepPlayer(string pos)
    {
        switch (pos)
        {
            case "L":
                beepPlayer.panStereo = -1f;
                break;
            case "M":
                beepPlayer.panStereo = 0f;
                break;
            case "R":
                beepPlayer.panStereo = 1f;
                break;
        }
    }

    private void PressButton(KMSelectable button)
    {
        button.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        if (moduleSolved || cantPress)
            return;
        var ix = Array.IndexOf(buttons, button);
        if (solution[stage] != ix)
        {
            module.HandleStrike();
            Debug.LogFormat("[Marco Polo #{0}] You pressed the button in the {1} position. That is incorrect. Strike!", moduleId, directionNames[ix]);
            StartCoroutine(FlashLeds());
            perfect = false;
        }
        else
        {
            leds[stage].material.color = on;
            stage++;
            Debug.LogFormat("[Marco Polo #{0}] You pressed the button in the {1} position. That is correct.", moduleId, directionNames[ix]);
            if (stage != 3)
                StartCoroutine(UpdateButtons());
            else
            {
                moduleSolved = true;
                cantPress = true;
                StartCoroutine(Solve());
                Debug.LogFormat("[Marco Polo #{0}] Module solved.", moduleId);
            }
        }
    }

    private IEnumerator Solve()
    {
        string[] solvedMessage = perfect ? new[] { "G", "G", "!" } : new[] { "O", "K", "." };
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(.3f);
            buttonTexts[i].text = "";
            audio.PlaySoundAtTransform("tap", buttons[i].transform);
            buttonTexts[i].color = perfect ? solvedColor : textColors[0];
        }
        yield return new WaitForSeconds(.4f);
        for (int i = 0; i < 3; i++)
        {
            if (i != 0)
                yield return new WaitForSeconds(.3f);
            buttonTexts[i].text = solvedMessage[i];
            audio.PlaySoundAtTransform("tap", buttons[i].transform);
        }
        StartCoroutine(FlashLeds());
        module.HandlePass();
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
    }

    private IEnumerator FlashLeds()
    {
        cantPress = true;
        for (int i = 0; i < 3; i++)
        {
            if (i != 0)
                yield return new WaitForSeconds(.2f);
            foreach (Renderer led in leds)
                led.material.color = off;
            yield return new WaitForSeconds(.2f);
            foreach (Renderer led in leds)
                led.material.color = moduleSolved ? on : strikeColor;
        }
        if (!moduleSolved)
        {
            for (int i = 0; i < 3; i++)
                if (i < stage)
                    leds[i].material.color = on;
                else
                    leds[i].material.color = off;
        }
        cantPress = false;
    }

    //twitch plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <pos> (#) [Presses the button in the specified position (green button only: optionally press '#' times with delays inbetween each press)] | Valid positions are L M, R, and G (Green)";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length == 3)
            {
                string[] valids3s = { "green", "g", "sound", "s" };
                parameters[1] = parameters[1].ToLower();
                if (valids3s.Contains(parameters[1]))
                {
                    int temp = 0;
                    bool check = int.TryParse(parameters[2], out temp);
                    if (check)
                    {
                        if (cantPress)
                        {
                            yield return "sendtochaterror Buttons cannot be pressed while the module is animating!";
                            yield break;
                        }
                        int counter = 0;
                        while (counter < temp)
                        {
                            counter++;
                            yield return "trycancel Halting pressing the green button '" + parameters[2] + "' times due to a request to cancel.";
                            soundButton.OnInteract();
                            yield return new WaitForSeconds(1f);
                        }
                    }
                    else
                    {
                        yield return "sendtochaterror The specified number of times to press the green button '" + parameters[2] + "' is invalid!";
                    }
                }
                else
                {
                    yield return "sendtochaterror Only the green button can be pressed a certain number of times!";
                }
            }
            else if (parameters.Length == 2)
            {
                string[] valids = { "green", "g", "sound", "s", "left", "l", "right", "r", "middle", "m", "center", "c" };
                parameters[1] = parameters[1].ToLowerInvariant();
                if (valids.Contains(parameters[1]))
                {
                    if (cantPress)
                    {
                        yield return "sendtochaterror Buttons cannot be pressed while the module is animating!";
                        yield break;
                    }
                    if (parameters[1].Equals("left") || parameters[1].Equals("l"))
                        buttons[0].OnInteract();
                    else if (parameters[1].Equals("middle") || parameters[1].Equals("m") || parameters[1].Equals("center") || parameters[1].Equals("c"))
                        buttons[1].OnInteract();
                    else if (parameters[1].Equals("right") || parameters[1].Equals("r"))
                        buttons[2].OnInteract();
                    else if (parameters[1].Equals("green") || parameters[1].Equals("g") || parameters[1].Equals("sound") || parameters[1].Equals("s"))
                        soundButton.OnInteract();
                    if (stage == 3)
                        yield return "solve";
                }
                else
                    yield return "sendtochaterror The specified position '" + parameters[1] + "' is invalid!";
            }
            else if (parameters.Length == 1)
                yield return "sendtochaterror Please specify which button to press!";
            else if (parameters.Length > 3)
                yield return "sendtochaterror Too many parameters!";
            yield break;
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (cantPress) { yield return true; }
        for (int i = stage; i < 3; i++)
        {
            buttons[solution[i]].OnInteract();
            while (cantPress) { yield return true; }
        }
    }
}