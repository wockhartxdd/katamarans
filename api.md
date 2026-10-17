# План API

- GET /catamarans — список катамаранов
- GET /catamarans/{id} — информация о катамаране
- POST /bookings — создать бронь
- GET /bookings/{id} — статус брони
- DELETE /bookings/{id} — отменить бронь

Дополнительно: GET /health — проверка работоспособности сервиса (для контейнера).

Все пять endpoint'ов из плана реализованы в Program.cs.
