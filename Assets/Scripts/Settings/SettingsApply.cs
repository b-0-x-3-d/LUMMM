using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SettingsApply : MonoBehaviour
{ 
    [SerializeField] AudioMixer musicMixer;
    [SerializeField] AudioMixer sfxMixer;

    // Start is called before the first frame update
    void Start()
    {
        InitMasterVolume();
        InitBGMVolume();
        InitSFXVolume();
        InitResolution();
        InitGraphicsQuality();
        InitInfiniteLives();
        InitCheckpoints();   
    }

    private void InitMasterVolume()
    {
        float volume = PlayerPrefs.GetFloat(SettingsKeys.MasterVolumeKey, 1f);
        AudioListener.volume = volume;
    }

    private void InitBGMVolume()
    {
        float volume = PlayerPrefs.GetFloat(SettingsKeys.BGMVolumeKey, 1f);
        musicMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20f);
    }

    private void InitSFXVolume()
    {
        float volume = PlayerPrefs.GetFloat(SettingsKeys.SFXVolumeKey, 1f);
        sfxMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20f);
    }

    private void InitResolution()
    {
        string savedResolution = PlayerPrefs.GetString(SettingsKeys.ResolutionKey, Screen.currentResolution.width + "x" + Screen.currentResolution.height);
        bool savedFullscreen = PlayerPrefs.GetInt(SettingsKeys.FullscreenKey, 1) == 1;
        
        string[] resolutionParts = savedResolution.Split('x');
        int savedWidth = int.Parse(resolutionParts[0]);
        int savedHeight = int.Parse(resolutionParts[1]);
        Resolution currentResolution = new Resolution { width = savedWidth, height = savedHeight };
        Screen.SetResolution(currentResolution.width, currentResolution.height, savedFullscreen);
    }

    private void InitGraphicsQuality()
    {
        int qualityLevel = PlayerPrefs.GetInt(SettingsKeys.GraphicsQualityKey, QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(qualityLevel);
    }

    private void InitInfiniteLives()
    {
        bool isInfiniteLivesEnabled = PlayerPrefs.GetInt(SettingsKeys.InfiniteLivesKey, 0) == 1;
        GlobalVariables.infiniteLivesMode = isInfiniteLivesEnabled;
    }

    private void InitCheckpoints()
    {
        bool areCheckpointsEnabled = PlayerPrefs.GetInt(SettingsKeys.CheckpointsKey, 0) == 1;
        GlobalVariables.enableCheckpoints = areCheckpointsEnabled;
    }  
}
