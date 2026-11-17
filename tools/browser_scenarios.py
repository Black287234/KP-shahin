"""Сценарии проверки модуля в браузере (Playwright) и через JSON API.

Запуск на сервере GitHub (процесс browser.yml) при работающем приложении:
  python tools/browser_scenarios.py main      — основные сценарии
  python tools/browser_scenarios.py restart   — проверка сохранности данных после перезапуска
  python tools/browser_scenarios.py tests <файл> — изображение с выводом dotnet test

Фактический результат каждого сценария считывается со страницы или из ответа API
и записывается в docs/verification/results.json; ожидаемый результат задан в сценарии заранее.
"""
import html
import json
import sys
import threading
import urllib.error
import urllib.request
from datetime import date, timedelta
from pathlib import Path

from playwright.sync_api import sync_playwright

BASE = "http://localhost:5085"
OUT = Path("docs/verification")
SHOTS = OUT / "shots"
SHOTS.mkdir(parents=True, exist_ok=True)
RESULTS = OUT / "results.json"


def target_day(offset):
    """Рабочий день (не воскресенье) не ранее чем через offset дней."""
    d = date.today() + timedelta(days=offset)
    while d.weekday() == 6:
        d += timedelta(days=1)
    return d


DAY = target_day(2)
DAY_ISO = DAY.isoformat()
TODAY_ISO = date.today().isoformat()


class Journal:
    def __init__(self):
        self.data = json.loads(RESULTS.read_text(encoding="utf-8")) if RESULTS.exists() else {"scenarios": []}
        self.data["day"] = DAY.strftime("%d.%m.%Y")

    def add(self, code, title, expected, actual, passed, shot=None):
        self.data["scenarios"] = [s for s in self.data["scenarios"] if s["code"] != code]
        self.data["scenarios"].append({"code": code, "title": title, "expected": expected,
                                       "actual": actual, "passed": bool(passed), "shot": shot})
        print(("ПРОЙДЕН " if passed else "НЕ ПРОЙДЕН ") + f"{code} {title}: {actual}")

    def save(self):
        self.data["scenarios"].sort(key=lambda s: s["code"])
        RESULTS.write_text(json.dumps(self.data, ensure_ascii=False, indent=2), encoding="utf-8")


def notes(page):
    texts = page.locator(".note.err, .note.ok, .validation-summary-errors").all_inner_texts()
    return " | ".join(" ".join(t.split()) for t in texts if t.strip())


def shot(page, name):
    page.screenshot(path=str(SHOTS / f"{name}.png"), full_page=True)
    return f"{name}.png"


def http(method, path, body=None, raw=None):
    data = raw.encode() if raw is not None else (json.dumps(body).encode() if body is not None else None)
    req = urllib.request.Request(BASE + path, data=data, method=method, headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=20) as r:
            return r.status, r.read().decode("utf-8")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8")


def short(status, text):
    try:
        j = json.loads(text)
        if isinstance(j, dict):
            text = j.get("error") or j.get("title") or json.dumps(j, ensure_ascii=False)
    except Exception:
        pass
    text = " ".join(text.split())
    return f"HTTP {status}: {text[:160] if text else '(пустой ответ)'}"


def room_ids():
    _, text = http("GET", "/api/rooms")
    return {r["code"]: r["id"] for r in json.loads(text)}


def fill_room(page, code, building, seats, kind="Classroom", projector=False):
    page.fill("#Input_Code", code)
    page.fill("#Input_Building", building)
    page.select_option("#Input_Kind", label=None, value=str(["Classroom", "LectureHall", "ComputerLab", "Laboratory", "AssemblyHall"].index(kind)))
    page.fill("#Input_Seats", str(seats))
    if projector:
        page.check("#Input_HasProjector")


def fill_booking(page, room_id, day, start, end, people=20, organizer="Расулов Ш. А.", group="ИСиП-23/1",
                 purpose="Практическое занятие по МДК.01.01"):
    page.goto(BASE + "/Reservations/New")
    page.select_option("#Input_RoomId", str(room_id))
    page.fill("#Input_Date", day)
    page.fill("#Input_From", start)
    page.fill("#Input_To", end)
    page.fill("#Input_Purpose", purpose)
    page.fill("#Input_Organizer", organizer)
    page.fill("#Input_StudyGroup", group)
    page.fill("#Input_Attendees", str(people))
    page.click("button[type=submit]")
    page.wait_for_load_state()


def main_scenarios(page, j):
    rooms = room_ids()
    b210, a112, a101 = rooms["Б-210"], rooms["А-112"], rooms["А-101"]

    page.goto(BASE + "/")
    tiles = " / ".join(" ".join(t.split()) for t in page.locator(".tile").all_inner_texts())
    j.add("С01", "Главная страница со сводкой на текущий день", "Отображаются четыре показателя и ближайшие бронирования",
          tiles, page.locator(".tile").count() == 4, shot(page, "01_home"))

    page.goto(BASE + "/Rooms?Kind=ComputerLab&Projector=true")
    found = [c.strip() for c in page.locator("table.grid tbody tr td:first-child").all_inner_texts()]
    j.add("С02", "Отбор аудиторий: компьютерный класс с проектором", "Найдена только аудитория А-112",
          "Найдены: " + ", ".join(found), found == ["А-112"], shot(page, "02_rooms_filter"))

    page.goto(BASE + "/Rooms/Edit")
    fill_room(page, "б-215", "Корпус Б", 26, projector=True)
    page.click("button[type=submit]")
    page.wait_for_load_state()
    title = page.locator("h1").inner_text()
    j.add("С03", "Регистрация аудитории с номером в нижнем регистре", "Аудитория зарегистрирована под номером Б-215",
          f"{notes(page)}; заголовок: {title}", "Б-215" in title, shot(page, "03_room_created"))

    page.goto(BASE + "/Rooms/Edit")
    fill_room(page, "Б-215", "Корпус Б", 20)
    page.click("button[type=submit]")
    page.wait_for_load_state()
    msg = notes(page)
    j.add("С04", "Повторная регистрация номера Б-215", "Отказ: аудитория уже зарегистрирована",
          msg, "уже зарегистрирована" in msg, shot(page, "04_room_duplicate"))

    page.goto(BASE + "/Rooms/Edit")
    fill_room(page, "A-101", "Корпус А", 40)   # латинская буква A
    page.click("button[type=submit]")
    page.wait_for_load_state()
    msg = notes(page) or page.locator("h1").inner_text()
    j.add("С05", "Номер A-101 с латинской буквой A (совпадает по написанию с А-101)",
          "Отказ: аудитория А-101 уже зарегистрирована", msg, "уже зарегистрирована" in msg, shot(page, "05_room_latin"))

    page.goto(BASE + "/Rooms/Edit")
    fill_room(page, "Б-216", "Корпус Б", 0)
    page.click("button[type=submit]")
    page.wait_for_load_state()
    msg = notes(page)
    j.add("С06", "Регистрация аудитории с количеством мест 0", "Отказ: количество мест от 1 до 500",
          msg, "от 1 до 500" in msg)

    fill_booking(page, b210, DAY_ISO, "10:10", "11:40", people=24)
    ok = "/Reservations/Card/" in page.url
    j.add("С07", f"Бронирование Б-210 на {DAY:%d.%m.%Y} 10:10–11:40", "Бронирование оформлено, открыта карточка",
          notes(page) + f"; адрес: {page.url.replace(BASE, '')}", ok, shot(page, "07_booking_created"))
    first_id = page.url.rstrip("/").split("/")[-1] if ok else None

    fill_booking(page, b210, DAY_ISO, "11:00", "12:00", people=24)
    msg = notes(page)
    alt = page.locator(".chip").count()
    j.add("С08", "Пересекающееся бронирование Б-210 11:00–12:00", "Отказ с указанием занятого времени и список свободных аудиторий",
          f"{msg}; предложено свободных аудиторий: {alt}", "уже занята" in msg and alt > 0, shot(page, "08_booking_conflict"))

    fill_booking(page, b210, DAY_ISO, "11:40", "12:20", people=24, purpose="Консультация")
    j.add("С09", "Смежное бронирование Б-210 11:40–12:20 (начинается в момент окончания предыдущего)",
          "Бронирование оформлено: смежные интервалы не пересекаются", notes(page) or page.url.replace(BASE, ""),
          "/Reservations/Card/" in page.url)

    fill_booking(page, b210, TODAY_ISO, "08:00", "08:30")
    msg = notes(page)
    j.add("С10", "Бронирование на прошедшее время (сегодня 08:00)", "Отказ: нельзя забронировать на прошедшее время",
          msg, "прошедшее" in msg)

    fill_booking(page, b210, DAY_ISO, "20:30", "21:30")
    msg = notes(page)
    j.add("С11", "Бронирование за пределами рабочего дня 20:30–21:30", "Отказ: бронирование возможно с 08:00 до 21:00",
          msg, "08:00 до 21:00" in msg, shot(page, "11_booking_hours"))

    fill_booking(page, a112, DAY_ISO, "17:20", "18:50", people=40)
    msg = notes(page)
    j.add("С12", "Участников 40 при 25 местах в А-112", "Отказ: участников больше, чем мест", msg, "больше, чем мест" in msg)

    if first_id:
        page.goto(f"{BASE}/Reservations/Card/{first_id}")
        page.click("form button.warn")
        page.wait_for_load_state()
        msg = notes(page)
        j.add("С13", "Отмена бронирования без указания причины", "Отказ: укажите причину отмены", msg, "причину" in msg)
        page.fill("#Reason", "Занятие перенесено в компьютерный класс")
        page.click("form button.warn")
        page.wait_for_load_state()
        state = page.locator(".head-row .tag").inner_text()
        j.add("С14", "Отмена бронирования с причиной", "Состояние «Отменено», причина сохранена",
              f"{notes(page)}; состояние: {state}", state == "Отменено", shot(page, "14_booking_cancelled"))
        fill_booking(page, b210, DAY_ISO, "10:10", "11:40", people=24, purpose="Лабораторная работа")
        j.add("С15", "Повторное бронирование освободившегося интервала Б-210 10:10–11:40",
              "Бронирование оформлено: отменённое бронирование не занимает время",
              notes(page) or page.url.replace(BASE, ""), "/Reservations/Card/" in page.url)

    page.goto(f"{BASE}/Schedule/Day?Date={DAY_ISO}")
    rows = page.locator(".sched-row").count() - 1
    busy = page.locator(".seg.busy").count()
    j.add("С16", f"Расписание занятости на {DAY:%d.%m.%Y}", "Шкала для каждой действующей аудитории, занятые отрезки — ссылки",
          f"аудиторий: {rows}, занятых отрезков: {busy}", rows >= 8 and busy > 0, shot(page, "16_schedule_day"))

    page.goto(f"{BASE}/Schedule/Week?RoomId={b210}&Date={DAY_ISO}")
    days = page.locator(".sched-row").count() - 1
    j.add("С17", "Недельное расписание аудитории Б-210", "Шесть дней (понедельник — суббота) и перечень бронирований",
          f"дней: {days}, строк в перечне: {page.locator('table.grid tbody tr').count()}", days == 6, shot(page, "17_schedule_week"))

    page.goto(f"{BASE}/Schedule/Free?Date={DAY_ISO}&From=10:30&To=11:00&Seats=25")
    found = [c.strip() for c in page.locator("table.grid tbody tr td:first-child").all_inner_texts()]
    j.add("С18", "Поиск свободных аудиторий на 10:30–11:00 не менее чем на 25 мест",
          "В списке нет Б-210 (занята) и аудиторий менее 25 мест", "Найдены: " + ", ".join(found),
          "Б-210" not in found and len(found) > 0, shot(page, "18_free_rooms"))

    # Вывод из эксплуатации аудитории с запланированными бронированиями
    page.goto(f"{BASE}/Rooms/Card/{b210}")
    page.fill("#Reason", "Замена оконных блоков")
    page.click("form button.warn")
    page.wait_for_load_state()
    msg = notes(page)
    _, text = http("GET", f"/api/reservations?roomId={b210}&from={DAY_ISO}")
    planned = sum(1 for r in json.loads(text) if r["state"] == "Planned")
    j.add("С19", "Вывод из эксплуатации аудитории Б-210, на которую есть запланированные бронирования",
          "Отказ с указанием числа запланированных бронирований", f"{msg}; запланировано бронирований: {planned}",
          "запланирован" in msg.lower(), shot(page, "19_room_out_with_bookings"))
    if "выведена из эксплуатации" in msg:   # вернуть состояние для следующих сценариев
        page.click("form button.btn")
        page.wait_for_load_state()

    # Уменьшение вместимости ниже числа участников запланированного бронирования
    page.goto(f"{BASE}/Rooms/Edit/{b210}")
    page.fill("#Input_Seats", "10")
    page.click("button[type=submit]")
    page.wait_for_load_state()
    msg = notes(page)
    j.add("С20", "Уменьшение числа мест Б-210 до 10 при бронировании на 24 участника",
          "Отказ: есть бронирования с большим числом участников", msg, "участник" in msg.lower(), shot(page, "20_room_seats"))
    if "сохранены" in msg:
        page.goto(f"{BASE}/Rooms/Edit/{b210}")
        page.fill("#Input_Seats", "32")
        page.click("button[type=submit]")

    # JSON API
    body = {"roomId": a101, "date": DAY_ISO, "from": "17:20", "to": "18:50", "organizer": "Иванов П. С.",
            "studyGroup": None, "purpose": "Открытая лекция", "attendees": 90}
    s1 = http("POST", "/api/reservations", body)
    s2 = http("POST", "/api/reservations", body)
    j.add("С21", "API: создание бронирования и повтор той же заявки",
          "Первый запрос — 201 Created, повторный — 409 Conflict с текстом ошибки",
          f"{short(*s1)} / {short(*s2)}", s1[0] == 201 and s2[0] == 409)

    s3 = http("POST", "/api/reservations", raw='{"roomId": 1, "date": "32.13.2026"')
    j.add("С22", "API: заявка с нарушенным форматом JSON и неверной датой",
          "400 Bad Request с текстом ошибки на русском языке", short(*s3),
          s3[0] == 400 and any("а" <= ch <= "я" for ch in s3[1].lower()))

    s4 = http("POST", "/api/reservations", {"roomId": a101, "date": DAY_ISO, "from": "19:00", "to": "20:00",
                                            "organizer": "", "purpose": "Лекция", "attendees": 10})
    j.add("С23", "API: заявка без ответственного", "422 с текстом «Укажите ответственного»", short(*s4),
          s4[0] == 422 and "ответственного" in s4[1])

    # Одновременная отправка десяти одинаковых заявок
    statuses = []
    body5 = dict(body, **{"from": "19:00", "to": "20:30", "purpose": "Репетиция", "attendees": 50})

    def send():
        statuses.append(http("POST", "/api/reservations", body5)[0])
    threads = [threading.Thread(target=send) for _ in range(10)]
    [t.start() for t in threads]
    [t.join() for t in threads]
    created = statuses.count(201)
    j.add("С24", "API: десять одновременных заявок на один интервал А-101",
          "Создано ровно одно бронирование, остальные отклонены (409)",
          f"201: {created}, 409: {statuses.count(409)}, прочие: {len(statuses) - created - statuses.count(409)}", created == 1)

    page.goto(BASE + "/Reservations/Card/999999")
    j.add("С25", "Открытие несуществующего бронирования", "Страница «Запись не найдена» (404)",
          page.locator("h1").inner_text(), "не найдена" in page.locator("h1").inner_text(), shot(page, "25_not_found"))

    page.goto(f"{BASE}/Reservations?From={DAY_ISO}&To={DAY_ISO}")
    j.add("С26", f"Журнал бронирований за {DAY:%d.%m.%Y}", "В журнале видны созданные и отменённые бронирования",
          f"записей: {page.locator('table.grid tbody tr').count()}, отменённых: {page.locator('tr.off').count()}",
          page.locator("tr.off").count() >= 1, shot(page, "26_journal"))

    j.data["created_room"] = "Б-215"


def restart_scenarios(page, j):
    page.goto(BASE + "/Rooms?Text=Б-215")
    found = page.locator("table.grid tbody tr").count()
    _, text = http("GET", f"/api/reservations?from={DAY_ISO}&to={DAY_ISO}")
    cancelled = sum(1 for r in json.loads(text) if r["state"] == "Cancelled")
    j.add("С27", "Сохранность данных после перезапуска приложения",
          "Аудитория Б-215 и отменённое бронирование сохранены в базе данных",
          f"аудитория Б-215 найдена: {'да' if found else 'нет'}; отменённых бронирований за день: {cancelled}",
          found == 1 and cancelled >= 1, shot(page, "27_after_restart"))


def tests_image(page, path):
    text = Path(path).read_text(encoding="utf-8", errors="replace")
    lines = [l for l in text.splitlines() if l.strip()][-60:]
    page.set_content("<pre style='font:13px Consolas,monospace;background:#0f1720;color:#d7e0e8;padding:16px;margin:0'>"
                     + html.escape("\n".join(lines)) + "</pre>")
    page.set_viewport_size({"width": 1200, "height": 400})
    page.screenshot(path=str(SHOTS / "30_tests.png"), full_page=True)


def run():
    mode = sys.argv[1]
    j = Journal()
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page(viewport={"width": 1280, "height": 860}, locale="ru-RU")
        if mode == "main":
            main_scenarios(page, j)
        elif mode == "restart":
            restart_scenarios(page, j)
        elif mode == "tests":
            tests_image(page, sys.argv[2])
        browser.close()
    j.save()


if __name__ == "__main__":
    run()
