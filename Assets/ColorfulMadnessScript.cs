using System.Collections.Generic;
using System.Linq;
using KMBombInfoHelper;
using UnityEngine;

public class ColorfulMadnessScript : MonoBehaviour
{
    public KMSelectable[] buttons;
    public KMBombInfo Info;

    public Texture2D[] colTexturesA = new Texture2D[1];
    public Texture2D[] colTexturesB = new Texture2D[1];
    int[] topHalfTextures = new int[10];
    int[] bottomHalfTextures = new int[10];

    int[] digits = new int[3];
    bool hasRY = false;
    int numCheckerboards = 0;
    bool hasSquare = false;

    List<int> pressedButtons = new List<int>();

    void Start()
    {
        var serialNum = Info.GetSerialNumber();

        for (int i = 0; i < 3; i++)
        {
            digits[i] = serialNum[i * 2];

            if (digits[i] >= 'A')
                digits[i] -= 'A';
            else
                digits[i] -= '0';
        }

        // Set up the top half of the board
        for (int i = 0; i < 10; i++)
        {
            do
                topHalfTextures[i] = Random.Range(0, 105);
            while (bottomHalfTextures.Contains(topHalfTextures[i]));

            if (topHalfTextures[i] <= 6)
                hasRY = true;

            if ((topHalfTextures[i] % 7) == 4)
                numCheckerboards++;

            if ((topHalfTextures[i] % 7) == 3)
                hasSquare = true;

            buttons[i].transform.GetChild(1).gameObject.GetComponent<Renderer>().material.SetTexture("_MainTex", colTexturesA[topHalfTextures[i]]);
            bottomHalfTextures[i] = topHalfTextures[i];
        }

        for (int v = 0; v < bottomHalfTextures.Length; v++)
        {
            int tmp = bottomHalfTextures[v];
            int r = Random.Range(v, bottomHalfTextures.Length);
            bottomHalfTextures[v] = bottomHalfTextures[r];
            bottomHalfTextures[r] = tmp;
        }

        for (int i = 10; i < 20; i++)
            buttons[i].transform.GetChild(1).gameObject.GetComponent<Renderer>().material.SetTexture("_MainTex", colTexturesB[bottomHalfTextures[i - 10]]);

        if (hasRY == true)
        {
            var batteryNum = Info.GetBatteryCount();
            for (int i = 0; i < 3; i++)
                digits[i] += batteryNum;
        }

        if (numCheckerboards > 0)
        {
            var portNum = Info.GetPortCount();
            var value = Mathf.Abs((numCheckerboards * 2) - portNum);
            for (int i = 0; i < 3; i++)
                digits[i] = Mathf.Abs(digits[i] - value);
        }

        if (hasSquare == true)
        {
            var holdersPlusPortPlates = Info.GetBatteryHolderCount() + Info.GetPortPlateCount();
            for (int i = 0; i < 3; i++)
                digits[i] *= holdersPlusPortPlates;
        }

        for (int i = 0; i < 3; i++)
            digits[i] = digits[i] % 10;

        while (digits[0] == digits[1] || digits[0] == digits[2])
            digits[0] = (digits[0] + 1) % 10;

        while (digits[1] == digits[0] || digits[1] == digits[2])
            digits[1] = (digits[1] + 9) % 10;

        for (int i = 0; i < buttons.Length; i++)
        {
            int j = i;

            buttons[i].OnInteract += delegate ()
            {
                onPress(j);
                return false;
            };
        }
    }

    void onPress(int pressed)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        for (int p = 0; p < pressedButtons.Count; p++)
        {
            if (pressed == pressedButtons[p])
            {
                // Correct button already pressed before: ignore
                return;
            }
        }

        if (
            // Pressed any of the correct buttons in the top half
            digits.Contains(pressed) ||
            // Pressed any of the correct counterparts in the bottom half
            (pressed >= 10 && digits.Any(digit => bottomHalfTextures[pressed - 10] == topHalfTextures[digit]))
        )
        {
            pressedButtons.Add(pressed);
            if (pressedButtons.Count == 6)
            {
                GetComponent<KMBombModule>().HandlePass();
            }
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
        }
    }
}