# Student API Route Inventory

Canonical full prefix:

```text
/api/v1/student
```

```text
GET    /ping
GET    /config
POST   /auth/login
POST   /auth/logout
GET    /bootstrap
GET    /profile
GET    /settings
PATCH  /settings
GET    /subjects
GET    /subjects/{subjectSlug}/terms
GET    /quizzes
GET    /quizzes/{quizId}
POST   /quizzes/{quizId}/attempts
GET    /quiz-results
GET    /quiz-results/{attemptId}
GET    /progress/summary
GET    /rewards
POST   /rewards/{rewardCode}/use
GET    /certificates
GET    /certificates/{certificateId}
GET    /announcements
GET    /leaderboards
GET    /sync/status
POST   /sync/push
GET    /missions
GET    /missions/{missionId}
GET    /missions/{missionId}/progress
POST   /missions/{missionId}/start
POST   /missions/{missionId}/areas/{areaId}/start
POST   /missions/{missionId}/areas/{areaId}/events
POST   /missions/{missionId}/areas/{areaId}/collectibles/{collectibleId}
POST   /missions/{missionId}/areas/{areaId}/complete
```

There are no final-challenge routes. Area 3 contains the integrated mission challenge.
