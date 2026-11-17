"""Проверка доступности страниц и адресов API работающего приложения.

Использование: python3 tools/check_pages.py http://localhost:5085
Для каждого адреса печатается фактический и ожидаемый код ответа; при расхождении
скрипт завершается с кодом 1 (процесс GitHub Actions считается неуспешным).
"""
import sys
import urllib.error
import urllib.parse
import urllib.request

CHECKS = [
    ("/", 200),
    ("/Rooms", 200),
    ("/Rooms?Text=а-1", 200),
    ("/Rooms?Kind=ComputerLab&MinSeats=20&Projector=true", 200),
    ("/Rooms?All=true", 200),
    ("/Rooms/Card/1", 200),
    ("/Rooms/Card/999", 404),
    ("/Rooms/Edit", 200),
    ("/Rooms/Edit/2", 200),
    ("/Rooms/Edit/999", 404),
    ("/api/rooms", 200),
    ("/api/rooms/1", 200),
    ("/api/rooms/999", 404),
    ("/no-such-page", 404),
]


def status(base, path):
    url = base + urllib.parse.quote(path, safe="/?=&")
    try:
        with urllib.request.urlopen(url, timeout=15) as r:
            return r.status
    except urllib.error.HTTPError as e:
        return e.code


def main():
    base = sys.argv[1].rstrip("/")
    failed = 0
    for path, expected in CHECKS:
        code = status(base, path)
        mark = "ПРОЙДЕНА" if code == expected else "ОШИБКА"
        failed += code != expected
        print(f"{mark:9} {code} (ожидался {expected})  {path}")
    print(f"Итого: {len(CHECKS) - failed} из {len(CHECKS)}")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
