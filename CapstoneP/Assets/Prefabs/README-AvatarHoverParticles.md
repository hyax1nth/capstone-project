Avatar Hover Particles Prefab

File: Assets/Prefabs/AvatarHoverParticles.prefab

Usage:
- Attach the prefab as the `avatarHoverParticles` field on `StudentDashboard` in the inspector.
- On the avatar GameObject (the one with the `Image` assigned to `avatarImage`) add an `EventTrigger` component and add two entries:
  - Pointer Enter -> StudentDashboard.OnAvatarPointerEnter()
  - Pointer Exit  -> StudentDashboard.OnAvatarPointerExit()
- Tweak particle settings (duration, color, emission) in the prefab inspector.

Testing LessonTemplate and Progress data

Overview:
- The `LessonTemplate` scene calls `ProgressService.WriteAttempt(...)` when the finish button is clicked. This writes a progress object per user at path:
  progress/{uid}/{subject}/{lessonId}

Schema written by ProgressService.WriteAttempt (example fields):
- unlocked: true
- completionStatus: bool (true if stars > 0)
- score: int (stars for the lesson, 0..3)
- timeSpentMs: long
- attemptsMade: int
- lastPlayedAt: unix ms
- correct: int
- incorrect: int
- lessonType: string

Additional updates:
- users/{uid}/stats updated with a JSON UserStats object that includes:
  - totalStars: cumulative stars (note: current implementation adds stars per attempt, not idempotent)
  - lessonPlayCounts: map of "{subject}/{lessonId}" -> count
  - mostPlayedLesson: string
  - completedLessons: list of completed lesson keys

What you'll see in the dashboard after testing:
- Lock overlay fades in/out for locked/unlocked lessons.
- Stars under each lesson button reflect the `score` value.
- Current lesson highlighted when launched via the dashboard (StudentDashboard sets LessonRegistry keys before loading).

Quick test steps:
1) Open the Dashboard scene in Editor. Ensure `StudentDashboard` inspector fields are set: avatarImage, avatarHoverParticles (optional), subject panels and lesson buttons.
2) Add children under a lesson button named `CurrentHighlight` (e.g., a small outline image) and `Stars` (a container with 3 star images as children).
3) Run the editor, sign in as a test user, open a subject, and click a lesson. The scene will load and the `LessonTemplate` finish button will record a fake attempt (3 correct -> 3 stars) and update progress.
4) Return to the dashboard. Refresh the subject panel to see the lock fade, stars, and highlights.

Notes and Improvements:
- The `totalStars` in `users/{uid}/stats` is incremented per attempt; if you want idempotent total (unique best-score per lesson), modify `ProgressService.UpdateUserStatsAfterAttempt` to calculate deltas using existing stored score.
- If your Unity version lacks `Object.FindAnyObjectByType`, replace with `FindObjectOfType<ProgressService>()` or wire a serialized reference.

If you want, I can:
- Add a small star sprite asset and example GameObject under `Assets/Prefabs` for easy wiring.
- Make the totalStars tracking idempotent (compute best per lesson) and persist correctly.
- Add debug UI to the dashboard to show `mostPlayedLesson` and `lessonPlayCounts` for the current user.
