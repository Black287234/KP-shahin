"""Рисунок: варианты взаимного расположения интервалов и результат проверки пересечения."""
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

plt.rcParams["font.family"] = "Liberation Serif"
cases = [
    ("Заканчивается в момент начала", 9.0, 10.0, False),
    ("Захватывает начало", 9.5, 10.5, True),
    ("Лежит внутри", 10.25, 11.0, True),
    ("Охватывает целиком", 9.0, 12.0, True),
    ("Захватывает окончание", 11.0, 12.0, True),
    ("Начинается в момент окончания", 11.5, 12.5, False),
]
fig, ax = plt.subplots(figsize=(9, 4.2), dpi=200)
ax.axvspan(10.0, 11.5, color="#0f5e5a", alpha=0.12)
ax.barh(len(cases), 1.5, left=10.0, height=0.55, color="#0f5e5a")
ax.text(10.75, len(cases), "существующее 10:00–11:30", ha="center", va="center", color="white", fontsize=9)
for i, (name, a, b, bad) in enumerate(cases):
    y = len(cases) - 1 - i
    ax.barh(y, b - a, left=a, height=0.45, color="#a12b24" if bad else "#1f6b35")
    ax.text(12.75, y, ("пересекается" if bad else "допустимо"), va="center", fontsize=9, color="#a12b24" if bad else "#1f6b35")
    ax.text(8.45, y, name, va="center", ha="right", fontsize=9)
ax.set_xlim(8.5, 14)
ax.set_xticks([9, 10, 11, 12, 13])
ax.set_xticklabels(["9:00", "10:00", "11:00", "12:00", "13:00"])
ax.set_yticks([])
for s in ("top", "right", "left"):
    ax.spines[s].set_visible(False)
plt.tight_layout()
plt.savefig("intervals.png", bbox_inches="tight")
