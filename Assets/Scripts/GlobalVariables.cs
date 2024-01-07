using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public static class GlobalVariables
{
    public static int lives = 3;
    public static int coinCount = 0;
    public static LevelInfo levelInfo;

    // The id of the last checkpoint the player touched
    // -1 means no checkpoint
    public static int checkpoint = -1;

    public static bool infiniteLivesMode = false;

    public static bool enableCheckpoints = false;

    public static bool enablePlushies = false;

    public static void ResetForLevel()
    {
        GlobalVariables.lives = levelInfo.lives;
        coinCount = 0;
        checkpoint = -1;
    }


    /* Editor Use Only */

    // Set to true to see what it looks like on mobile
    public static bool forceMobileMode = false;
}