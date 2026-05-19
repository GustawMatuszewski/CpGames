using UnityEngine;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
//  SleepManager  —  Singleton koordynujący sekwencję snu
//
//  SCENE SETUP
//  ┌─ SleepManager GameObject  [SleepManager script]
//
//  Fade UI:
//  Skrypt oczekuje CanvasGroup o nazwie fadeCanvasGroup do obsługi
//  przyciemnienia ekranu. Podepnij go w Inspektorze (np. pełnoekranowy
//  czarny panel z CanvasGroup — alpha domyślnie 0, Interactable OFF,
//  Blocks Raycasts OFF). UI Toolkit zajmie się docelowym wyglądem —
//  tutaj tylko minimalna logika alpha.
// ─────────────────────────────────────────────────────────────────────────────
public class SleepManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static SleepManager Instance { get; private set; }

    // ── Fade ──────────────────────────────────────────────────────
    [Header("Fade (opcjonalne — podepnij panel z CanvasGroup)")]
    [Tooltip("CanvasGroup czarnego panelu fade. Może być null — wtedy fade jest pomijany.")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("Czas trwania jednej fazy fade (ściemnianie lub rozjaśnianie) w sekundach.")]
    public float fadeDuration = 0.8f;

    [Tooltip("Ile sekund ekran pozostaje czarny między ściemnieniem a rozjaśnieniem.")]
    public float blackScreenHold = 0.5f;

    // ── Stan wewnętrzny ───────────────────────────────────────────
    bool _isSleeping = false;

    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SleepManager] Duplikat Singletona — niszczę nadmiarowy obiekt.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Upewniamy się, że fade panel startuje przezroczysty
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0f;
    }

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Uruchamia pełną sekwencję snu.
    /// Wywoływane przez Bed.OnInteract().
    /// </summary>
    /// <param name="playerStatus">EntityStatus gracza (źródło statystyk + KCC)</param>
    /// <param name="hours">Liczba godzin snu (z Bed.hoursToSleep)</param>
    public void ExecuteSleep(EntityStatus playerStatus, int hours)
    {
        if (_isSleeping)
        {
            Debug.Log("[SleepManager] Sekwencja snu już trwa — ignoruję.");
            return;
        }

        StartCoroutine(SleepRoutine(playerStatus, hours));
    }

    // ─────────────────────────────────────────────────────────────
    //  COROUTINE GŁÓWNA
    // ─────────────────────────────────────────────────────────────

    IEnumerator SleepRoutine(EntityStatus playerStatus, int hours)
    {
        _isSleeping = true;

        // 1) Pobierz KCC z tego samego GameObject co EntityStatus
        KCC kcc = playerStatus.GetComponent<KCC>();

        // ── Blokuj ruch gracza ────────────────────────────────────
        if (kcc != null)
        {
            kcc.enableMovement  = false;
            kcc.enableClimbing  = false;
        }

        // 2) Ściemnianie ekranu (fade-out)
        yield return StartCoroutine(Fade(from: 0f, to: 1f));

        // 3) Na czarnym ekranie: przesuń czas + zmień pogodę + zregeneruj statystyki.
        //    Kolejność jest ważna — najpierw czas (EnvironmentManager),
        //    potem statystyki (bo EntityStatus zależy od aktualnego czasu).
        //    Pogoda snapuje natychmiast — gracz i tak nic nie widzi, więc
        //    nie ma potrzeby lerpa; budzi się już w nowych warunkach.
        if (EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.SkipTime(hours);
            EnvironmentManager.Instance.SnapRandomWeather();
        }
        else
            Debug.LogWarning("[SleepManager] Brak EnvironmentManager.Instance — pomijam przesunięcie czasu.");

        playerStatus.RegenerateStatsFromSleep(hours);

        // 4) Odczekaj chwilę na czarnym ekranie (poczucie upływu czasu)
        yield return new WaitForSeconds(blackScreenHold);

        // 5) Rozjaśnianie ekranu (fade-in)
        yield return StartCoroutine(Fade(from: 1f, to: 0f));

        // ── Przywróć ruch gracza ──────────────────────────────────
        if (kcc != null)
        {
            kcc.enableMovement  = true;
            kcc.enableClimbing  = true;
        }

        _isSleeping = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  FADE HELPER
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Interpoluje alpha CanvasGroup od <paramref name="from"/> do <paramref name="to"/>
    /// przez czas <see cref="fadeDuration"/>. Jeśli fadeCanvasGroup jest null, natychmiast wraca.
    /// </summary>
    IEnumerator Fade(float from, float to)
    {
        if (fadeCanvasGroup == null)
            yield break;

        float elapsed = 0f;
        fadeCanvasGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }
}