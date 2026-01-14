using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public static class FMODUtility
{
    // ---------------------------------------------------------
    // PLAY ONE SHOT (Non-positional)
    // ---------------------------------------------------------
    public static void PlayOneShot(string eventPath)
    {
        RuntimeManager.PlayOneShot(eventPath);
    }

    // ---------------------------------------------------------
    // PLAY ONE SHOT AT POSITION
    // ---------------------------------------------------------
    public static void PlayOneShot(string eventPath, Vector3 pos)
    {
        RuntimeManager.PlayOneShot(eventPath, pos);
    }

    // ---------------------------------------------------------
    // CREATE PERSISTENT EVENT
    // ---------------------------------------------------------
    public static EventInstance CreateInstance(string eventPath)
    {
        EventInstance instance = RuntimeManager.CreateInstance(eventPath);
        return instance;
    }

    // ---------------------------------------------------------
    // START EVENT (if not already playing)
    // ---------------------------------------------------------
    public static void StartEvent(ref EventInstance instance)
    {
        instance.start();
    }

    // ---------------------------------------------------------
    // STOP EVENT (Allow fade out)
    // ---------------------------------------------------------
    public static void StopEvent(ref EventInstance instance)
    {
        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }

    // ---------------------------------------------------------
    // STOP EVENT IMMEDIATELY
    // ---------------------------------------------------------
    public static void StopEventImmediate(ref EventInstance instance)
    {
        instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }

    // ---------------------------------------------------------
    // SET A PARAMETER (by name)
    // ---------------------------------------------------------
    public static void SetParameter(ref EventInstance instance, string parameter, float value)
    {
        instance.setParameterByName(parameter, value);
    }

    // ---------------------------------------------------------
    // SET GLOBAL PARAMETER (affects all events)
    // ---------------------------------------------------------
    public static void SetGlobalParameter(string name, float value)
    {
        RuntimeManager.StudioSystem.setParameterByName(name, value);
    }

    // ---------------------------------------------------------
    // GET PARAMETER VALUE (from event)
    // ---------------------------------------------------------
    public static float GetParameter(ref EventInstance instance, string parameter)
    {
        float val;
        instance.getParameterByName(parameter, out val);
        return val;
    }

    // ---------------------------------------------------------
    // BUS VOLUME
    // ---------------------------------------------------------
    public static void SetBusVolume(string busPath, float volume)
    {
        Bus bus = RuntimeManager.GetBus(busPath);
        bus.setVolume(volume);
    }

    // ---------------------------------------------------------
    // VCA CONTROL
    // ---------------------------------------------------------
    public static void SetVCA(string vcaPath, float volume)
    {
        VCA vca = RuntimeManager.GetVCA(vcaPath);
        vca.setVolume(volume);
    }

    // ---------------------------------------------------------
    // SNAPSHOT CONTROL
    // ---------------------------------------------------------
    public static EventInstance StartSnapshot(string snapshotPath)
    {
        EventInstance instance = RuntimeManager.CreateInstance(snapshotPath);
        instance.start();
        return instance;
    }

    public static void StopSnapshot(ref EventInstance snapshot)
    {
        snapshot.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }

    // ---------------------------------------------------------
    // BANK LOADING / UNLOADING
    // ---------------------------------------------------------
    public static void LoadBank(string bankName)
    {
        RuntimeManager.StudioSystem.loadBankFile(bankName, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out _);
    }

    public static void UnloadBank(string bankName)
    {
        Bank bank;
        RuntimeManager.StudioSystem.getBank(bankName, out bank);
        bank.unload();
    }
}