using KMBombInfoHelper;
using UnityEngine;

public class ColorfulMadnessScript : MonoBehaviour
{
    public KMSelectable[] buttons;
    public KMBombInfo Info;

    bool isActivated = false;
    int correctIndex;
    int canPress = 0;
    bool pairPress;

    public Texture2D[] colTexturesA = new Texture2D[1];
    public Texture2D[] colTexturesB = new Texture2D[1];
    int setTexOver = 0;
    int[] checkTexOver = new int[10];
    int[] texOverVal = new int[10];

    string serialNum = "";
    int batteryNum = 0;
    int portNum = 0;
    int holdersNum = 0;
    int[] digits = new int[3];
    char[] serialDigit = new char[3];
    bool hasRY = false;
    int hasMesh = 0;
    bool hasSquare = false;

    int[] pressedButtons = new int[6];
    int nowPressed = 0;

    void Start()
    {
        Init();

        GetComponent<KMBombModule>().OnActivate += ActivateModule;
    }

    void Init()
    {
        serialNum = Info.GetSerialNumber();
        batteryNum = Info.GetBatteryCount();
        portNum = Info.GetPortCount();
        holdersNum = Info.GetBatteryHolderCount() + Info.GetPortPlateCount();

        for (int i = 0; i < 3; i++)
        {
            int j = i;

            serialDigit[j] = serialNum[j * 2];
            digits[j] = serialDigit[j];

            if (digits[j] >= 65)
            {
                digits[j] -= 65;
            }
            else
            {
                if (digits[j] >= 48)
                {
                    digits[j] -= 48;
                }
            }
        }

        for (int k = 0; k < 2; k++)
        {
            if (k == 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    int j = i;

                    if (j != 0)
                    {
                        setTexOver = texOverVal[j - 1];
                    }

                    while (setTexOver == texOverVal[0] || setTexOver == texOverVal[1] || setTexOver == texOverVal[2] || setTexOver == texOverVal[3] || setTexOver == texOverVal[4] || setTexOver == texOverVal[5] || setTexOver == texOverVal[6] || setTexOver == texOverVal[7] || setTexOver == texOverVal[8] || setTexOver == texOverVal[9])
                    {
                        setTexOver = Random.Range(0, 105);
                    }

                    checkTexOver[j] = setTexOver;

                    if (setTexOver <= 6)
                    {
                        hasRY = true;
                    }

                    if ((setTexOver % 7) == 4)
                    {
                        hasMesh++;
                    }

                    if ((setTexOver % 7) == 3)
                    {
                        hasSquare = true;
                    }

                    buttons[j].transform.GetChild(1).gameObject.GetComponent<Renderer>().material.SetTexture("_MainTex", colTexturesA[setTexOver]);
                    texOverVal[j] = setTexOver;
                }
            }
            else
            {
                for (int v = 0; v < texOverVal.Length; v++)
                {
                    int tmp = texOverVal[v];
                    int r = Random.Range(v, texOverVal.Length);
                    texOverVal[v] = texOverVal[r];
                    texOverVal[r] = tmp;
                }

                for (int i = 10; i < 20; i++)
                {
                    int j = i;

                    buttons[j].transform.GetChild(1).gameObject.GetComponent<Renderer>().material.SetTexture("_MainTex", colTexturesB[texOverVal[j - 10]]);
                }
            }
        }

        for (int i = 0; i < 3; i++)
        {
            int j = i;

            if (hasRY == true)
            {
                digits[j] += batteryNum;
            }

            if (hasMesh > 0)
            {
                int meshPort = Mathf.Abs((hasMesh * 2) - portNum);

                digits[j] -= meshPort;
                digits[j] = Mathf.Abs(digits[j]);
            }

            if (hasSquare == true)
            {
                digits[j] *= holdersNum;
            }

            digits[j] = digits[j] % 10;
        }

        while (digits[0] == digits[1] || digits[0] == digits[2])
        {
            digits[0]++;

            if (digits[0] > 9)
            {
                digits[0] = 0;
            }
        }

        while (digits[1] == digits[0] || digits[1] == digits[2])
        {
            digits[1]--;

            if (digits[1] < 0)
            {
                digits[1] = 9;
            }
        }

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

    void ActivateModule()
    {
        isActivated = true;
    }

    void onPress(int pressed)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        canPress = 0;
        pairPress = false;

        for (int p = 0; p < nowPressed; p++)
        {
            if (pressed == pressedButtons[p])
            {
                canPress++;
            }
        }

        if (canPress == 0)
        {
            if (pressed >= 10)
            {
                pairPress = (texOverVal[pressed - 10] == checkTexOver[digits[0]] || texOverVal[pressed - 10] == checkTexOver[digits[1]] || texOverVal[pressed - 10] == checkTexOver[digits[2]]);
            }

            if ((pressed == digits[0] || pressed == digits[1] || pressed == digits[2]) || pairPress == true)
            {
                pressedButtons[nowPressed] = pressed;
                nowPressed++;

                if (nowPressed == 6)
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
}