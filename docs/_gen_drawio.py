#!/usr/bin/env python3
"""Generate CleanDistrict diagrams as diagrams.net (.drawio) multi-page file."""
from __future__ import annotations

from pathlib import Path
from xml.sax.saxutils import escape


def cell(
    cid: str,
    parent: str,
    style: str,
    value: str,
    x: float = 0,
    y: float = 0,
    w: float = 0,
    h: float = 0,
    *,
    vertex: bool = True,
    edge: bool = False,
    source: str | None = None,
    target: str | None = None,
    points: list[tuple[float, float]] | None = None,
) -> str:
    attrs = [f'id="{cid}"', f'parent="{parent}"', f'style="{style}"', f'value="{escape(value)}"']
    if vertex and not edge:
        attrs.append('vertex="1"')
    if edge:
        attrs.append('edge="1"')
        if source:
            attrs.append(f'source="{source}"')
        if target:
            attrs.append(f'target="{target}"')
        geom = '<mxGeometry relative="1" as="geometry"'
        if points:
            pts = "".join(f'<mxPoint x="{px}" y="{py}" />' for px, py in points)
            geom += f'><Array as="points">{pts}</Array></mxGeometry>'
        else:
            geom += " />"
        return f'<mxCell {" ".join(attrs)}>{geom}</mxCell>'
    return (
        f'<mxCell {" ".join(attrs)}>'
        f'<mxGeometry x="{x}" y="{y}" width="{w}" height="{h}" as="geometry" />'
        f"</mxCell>"
    )


def page(name: str, pid: str, cells: list[str], width: int = 1200, height: int = 800) -> str:
    body = "\n        ".join(cells)
    return f'''  <diagram name="{escape(name)}" id="{pid}">
    <mxGraphModel dx="1200" dy="800" grid="1" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" fold="1" page="1" pageScale="1" pageWidth="{width}" pageHeight="{height}" math="0" shadow="0">
      <root>
        {body}
      </root>
    </mxGraphModel>
  </diagram>'''


EDGE = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;endArrow=block;endFill=1;"
EDGE_NONE = "endArrow=none;html=1;rounded=0;"
ACTOR = "shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;outlineConnect=0;"
UC = "ellipse;whiteSpace=wrap;html=1;fillColor=#dae8fc;strokeColor=#6c8ebf;"
STATE = "rounded=1;whiteSpace=wrap;html=1;fontStyle=1;fillColor=#fff2cc;strokeColor=#d6b656;"
STATE2 = "rounded=1;whiteSpace=wrap;html=1;fontStyle=1;fillColor=#dae8fc;strokeColor=#6c8ebf;"
STATE3 = "rounded=1;whiteSpace=wrap;html=1;fontStyle=1;fillColor=#d5e8d4;strokeColor=#82b366;"
STATE4 = "rounded=1;whiteSpace=wrap;html=1;fontStyle=1;fillColor=#ffe6cc;strokeColor=#d79b00;"
NOTE = "shape=note;whiteSpace=wrap;html=1;size=14;fillColor=#e1d5e7;strokeColor=#9673a6;"
TITLE = "text;html=1;strokeColor=none;fillColor=none;align=left;fontStyle=1;fontSize=16;"
BOX = "rounded=0;whiteSpace=wrap;html=1;fillColor=#f5f5f5;strokeColor=#666666;verticalAlign=top;fontStyle=1;"
HDR = "rounded=0;whiteSpace=wrap;html=1;fillColor=#e1d5e7;strokeColor=#9673a6;fontStyle=1;"
ENTITY = "shape=mxgraph.er.entity;whiteSpace=wrap;html=1;align=center;fillColor=#dae8fc;strokeColor=#6c8ebf;"
START = "ellipse;html=1;aspect=fixed;fillColor=#000000;strokeColor=#000000;"
END = "ellipse;html=1;shape=endState;fillColor=#000000;strokeColor=#ffffff;aspect=fixed;"


def build() -> str:
    pages: list[str] = []

    # ---- 1. Use cases ----
    p = "uc"
    cells = ['<mxCell id="0" />', '<mxCell id="1" parent="0" />']
    cells.append(cell(f"{p}-title", "1", TITLE, "Диаграмма прецедентов CleanDistrict", 40, 10, 500, 30))
    cells.append(cell(f"{p}-sys", "1", BOX, "Система «Чистый округ»", 260, 50, 780, 760))

    actors = [
        ("client", "Клиент", 60, 100),
        ("manager", "Менеджер", 60, 260),
        ("dispatcher", "Диспетчер", 60, 420),
        ("crew", "Бригадир", 60, 560),
        ("mechanic", "Механик", 60, 680),
        ("admin", "Администратор", 1120, 180),
        ("director", "Руководитель", 1120, 420),
    ]
    for aid, name, x, y in actors:
        cells.append(cell(f"{p}-a-{aid}", "1", ACTOR, name, x, y, 40, 80))

    ucs = [
        ("c1", "Создать заявку (портал)", 320, 70),
        ("c2", "Мои заявки", 320, 150),
        ("c3", "Отменить новую заявку", 320, 230),
        ("m1", "Выдать доступ клиенту", 520, 70),
        ("m2", "Вести клиентов и объекты", 520, 150),
        ("m3", "Вести договоры", 520, 230),
        ("m4", "Создать расчёт", 520, 310),
        ("m5", "Создать заявку (сотрудник)", 520, 390),
        ("m6", "Согласовать клиентскую", 520, 470),
        ("m7", "Закрыть заявку", 520, 550),
        ("m8", "Отчёты / CSV", 520, 630),
        ("d1", "Создать наряд", 720, 390),
        ("d2", "Отменить заявку", 720, 470),
        ("cr1", "Начать работы", 720, 550),
        ("cr2", "Сдать отчёт", 720, 630),
        ("f1", "Вести технику", 720, 710),
        ("f2", "ТО / ремонт / списание", 880, 710),
        ("ad1", "Справочники и пользователи", 880, 180),
    ]
    for uid, name, x, y in ucs:
        cells.append(cell(f"{p}-u-{uid}", "1", UC, name, x, y, 160, 60))

    edges = [
        ("client", "c1"), ("client", "c2"), ("client", "c3"),
        ("manager", "m1"), ("manager", "m2"), ("manager", "m3"), ("manager", "m4"),
        ("manager", "m5"), ("manager", "m6"), ("manager", "m7"), ("manager", "m8"),
        ("dispatcher", "m5"), ("dispatcher", "m6"), ("dispatcher", "d1"), ("dispatcher", "d2"),
        ("crew", "cr1"), ("crew", "cr2"),
        ("mechanic", "f1"), ("mechanic", "f2"),
        ("admin", "ad1"), ("admin", "m2"), ("admin", "d1"), ("admin", "f1"), ("admin", "m8"),
        ("director", "m8"), ("director", "m2"),
    ]
    for i, (src, tgt) in enumerate(edges):
        style = EDGE_NONE + ("dashed=1;" if src in ("admin", "director") and tgt != "ad1" else "")
        cells.append(
            cell(f"{p}-e-{i}", "1", style, "", edge=True, vertex=False,
                 source=f"{p}-a-{src}", target=f"{p}-u-{tgt}")
        )
    pages.append(page("1. Диаграмма прецедентов", "page-uc", cells, 1400, 900))

    # ---- 2. Request lifecycle ----
    p = "req"
    cells = ['<mxCell id="0" />', '<mxCell id="1" parent="0" />']
    cells.append(cell(f"{p}-title", "1", TITLE, "Жизненный цикл заявки", 40, 20, 400, 30))
    cells.append(cell(f"{p}-start", "1", START, "", 40, 145, 20, 20))
    states = [
        ("new", "Новая", 100, 120),
        ("approved", "Согласована", 300, 120),
        ("assigned", "Назначена", 500, 120),
        ("progress", "В работе", 700, 120),
        ("done", "Выполнена", 900, 120),
        ("closed", "Закрыта", 1100, 120),
        ("cancel", "Отменена", 500, 300),
    ]
    for sid, name, x, y in states:
        cells.append(cell(f"{p}-s-{sid}", "1", STATE, name, x, y, 150, 60))
    cells.append(cell(f"{p}-end1", "1", END, "", 1145, 230, 24, 24))
    cells.append(cell(f"{p}-end2", "1", END, "", 555, 400, 24, 24))
    trans = [
        ("start", "new", "Создание\n(сотрудник / клиент)"),
        ("new", "approved", "Согласование\n(+ тариф для клиента)"),
        ("approved", "assigned", "Создание наряда"),
        ("assigned", "progress", "Бригадир начал"),
        ("progress", "done", "Отчёт бригадира"),
        ("done", "closed", "Закрытие"),
        ("closed", "end1", ""),
        ("new", "cancel", "Отмена"),
        ("approved", "cancel", "Отмена"),
        ("assigned", "cancel", "Отмена (+ наряд)"),
        ("cancel", "end2", ""),
    ]
    for i, (src, tgt, label) in enumerate(trans):
        s = f"{p}-start" if src == "start" else (f"{p}-{src}" if src.startswith("end") else f"{p}-s-{src}")
        t = f"{p}-{tgt}" if tgt.startswith("end") else f"{p}-s-{tgt}"
        cells.append(cell(f"{p}-e-{i}", "1", EDGE, label, edge=True, vertex=False, source=s, target=t))
    cells.append(cell(
        f"{p}-note", "1", NOTE,
        "Staff: тариф сразу при создании.\nClient: тариф и стоимость назначает сотрудник при согласовании.",
        100, 420, 320, 90,
    ))
    pages.append(page("2. Жизненный цикл заявки", "page-req", cells, 1400, 600))

    # ---- 3. Work order ----
    p = "wo"
    cells = ['<mxCell id="0" />', '<mxCell id="1" parent="0" />']
    cells.append(cell(f"{p}-title", "1", TITLE, "Жизненный цикл наряда", 40, 20, 400, 30))
    cells.append(cell(f"{p}-start", "1", START, "", 40, 145, 20, 20))
    for sid, name, x, y in [
        ("planned", "Плановый", 100, 120),
        ("progress", "В работе", 340, 120),
        ("done", "Выполнен", 580, 120),
        ("cancel", "Отменён", 340, 280),
    ]:
        cells.append(cell(f"{p}-s-{sid}", "1", STATE2, name, x, y, 160, 60))
    cells.append(cell(f"{p}-end1", "1", END, "", 800, 138, 24, 24))
    cells.append(cell(f"{p}-end2", "1", END, "", 400, 380, 24, 24))
    for i, (src, tgt, label) in enumerate([
        ("start", "planned", "Создание диспетчером"),
        ("planned", "progress", "Start бригадира"),
        ("progress", "done", "Отчёт"),
        ("done", "end1", ""),
        ("planned", "cancel", "Отмена заявки"),
        ("cancel", "end2", ""),
    ]):
        s = f"{p}-start" if src == "start" else f"{p}-s-{src}"
        t = f"{p}-{tgt}" if tgt.startswith("end") else f"{p}-s-{tgt}"
        cells.append(cell(f"{p}-e-{i}", "1", EDGE, label, edge=True, vertex=False, source=s, target=t))
    pages.append(page("3. Жизненный цикл наряда", "page-wo", cells, 1000, 480))

    # ---- 4. Vehicle ----
    p = "veh"
    cells = ['<mxCell id="0" />', '<mxCell id="1" parent="0" />']
    cells.append(cell(f"{p}-title", "1", TITLE, "Состояния техники", 40, 20, 400, 30))
    cells.append(cell(f"{p}-start", "1", START, "", 40, 200, 20, 20))
    for sid, name, x, y in [
        ("avail", "Доступна", 120, 180),
        ("assigned", "Назначена", 380, 60),
        ("maint", "На ТО", 380, 180),
        ("repair", "В ремонте", 380, 300),
        ("dead", "Списана", 640, 180),
    ]:
        cells.append(cell(f"{p}-s-{sid}", "1", STATE3, name, x, y, 160, 60))
    cells.append(cell(f"{p}-end", "1", END, "", 860, 198, 24, 24))
    for i, (src, tgt, label) in enumerate([
        ("start", "avail", "Создание"),
        ("avail", "assigned", "В наряд"),
        ("assigned", "avail", "Конец/отмена наряда"),
        ("avail", "maint", "Старт ТО"),
        ("maint", "avail", "Завершение ТО"),
        ("avail", "repair", "Ремонт"),
        ("repair", "avail", "Завершение ремонта"),
        ("avail", "dead", "Списание"),
        ("dead", "end", ""),
    ]):
        s = f"{p}-start" if src == "start" else f"{p}-s-{src}"
        t = f"{p}-end" if tgt == "end" else f"{p}-s-{tgt}"
        cells.append(cell(f"{p}-e-{i}", "1", EDGE, label, edge=True, vertex=False, source=s, target=t))
    cells.append(cell(
        f"{p}-note", "1", "shape=note;whiteSpace=wrap;html=1;size=14;fillColor=#f8cecc;strokeColor=#b85450;",
        "Нельзя начать ТО, ремонт или списание при активном наряде.\nНельзя назначить недоступную / на ТО / в ремонте / списанную.",
        120, 400, 440, 80,
    ))
    pages.append(page("4. Состояния техники", "page-veh", cells, 1000, 560))

    # ---- 5. Maintenance ----
    p = "mnt"
    cells = ['<mxCell id="0" />', '<mxCell id="1" parent="0" />']
    cells.append(cell(f"{p}-title", "1", TITLE, "Жизненный цикл ТО", 40, 20, 400, 30))
    cells.append(cell(f"{p}-start", "1", START, "", 40, 145, 20, 20))
    for sid, name, x in [("planned", "Запланировано", 100), ("progress", "Выполняется", 340), ("done", "Завершено", 580)]:
        cells.append(cell(f"{p}-s-{sid}", "1", STATE4, name, x, 120, 180, 60))
    cells.append(cell(f"{p}-end", "1", END, "", 820, 138, 24, 24))
    for i, (src, tgt, label) in enumerate([
        ("start", "planned", "CreateMaintenance"),
        ("planned", "progress", "StartMaintenance"),
        ("progress", "done", "Complete (+90 д / +10 000 км / +500 мч)"),
        ("done", "end", ""),
    ]):
        s = f"{p}-start" if src == "start" else f"{p}-s-{src}"
        t = f"{p}-end" if tgt == "end" else f"{p}-s-{tgt}"
        cells.append(cell(f"{p}-e-{i}", "1", EDGE, label, edge=True, vertex=False, source=s, target=t))
    pages.append(page("5. Жизненный цикл ТО", "page-mnt", cells, 1000, 320))

    # ---- 6. Sequence staff ----
    p = "seq"
    cells = ['<mxCell id="0" />', '<mxCell id="1" parent="0" />']
    cells.append(cell(f"{p}-title", "1", TITLE, "Последовательность: заявка → закрытие", 40, 20, 520, 30))
    cols = [("M", "Менеджер / Диспетчер", 60), ("S", "Система", 300), ("D", "Диспетчер", 540), ("C", "Бригадир", 780)]
    for aid, name, x in cols:
        cells.append(cell(f"{p}-h-{aid}", "1", HDR, name, x, 60, 160, 40))
        cells.append(cell(f"{p}-b-{aid}", "1", "ellipse;fillColor=none;strokeColor=none;", "", x + 75, 560, 10, 10))
        cells.append(cell(
            f"{p}-ll-{aid}", "1", "endArrow=none;html=1;dashed=1;dashPattern=8 8;strokeColor=#999999;",
            "", edge=True, vertex=False, source=f"{p}-h-{aid}", target=f"{p}-b-{aid}",
        ))
    msgs = [
        (130, "M", "S", "1. Создать заявку / ProcessClient → Согласована"),
        (190, "S", "S", "2. SignalR RequestChanged"),
        (250, "D", "S", "3. Создать наряд (бригада, техника, интервал)"),
        (310, "S", "D", "4. OK: Назначена / иначе ошибка пересечения"),
        (370, "C", "S", "5. Начать работы → В работе"),
        (430, "C", "S", "6. Отчёт + фото → Выполнена"),
        (490, "M", "S", "7. Закрыть заявку → Закрыта"),
    ]
    xmap = {aid: x for aid, _, x in cols}
    for i, (y, src, tgt, label) in enumerate(msgs):
        cells.append(cell(f"{p}-ms-{i}", "1", "ellipse;fillColor=#ffffff;strokeColor=#666666;", "", xmap[src] + 75, y, 10, 10))
        cells.append(cell(f"{p}-mt-{i}", "1", "ellipse;fillColor=#ffffff;strokeColor=#666666;", "", xmap[tgt] + 75, y, 10, 10))
        cells.append(cell(
            f"{p}-me-{i}", "1",
            "endArrow=block;html=1;rounded=0;endFill=1;strokeWidth=1;",
            label, edge=True, vertex=False, source=f"{p}-ms-{i}", target=f"{p}-mt-{i}",
        ))
    pages.append(page("6. Последовательность заявки", "page-seq", cells, 1100, 640))

    # ---- 7. Client sequence ----
    p = "cseq"
    cells = ['<mxCell id="0" />', '<mxCell id="1" parent="0" />']
    cells.append(cell(f"{p}-title", "1", TITLE, "Последовательность: клиентский портал", 40, 20, 520, 30))
    cols = [("A", "Админ / Менеджер", 60), ("K", "Клиент", 300), ("P", "Портал", 540), ("R", "Заявки (сотрудники)", 780)]
    for aid, name, x in cols:
        cells.append(cell(f"{p}-h-{aid}", "1", HDR, name, x, 60, 170, 40))
        cells.append(cell(f"{p}-b-{aid}", "1", "ellipse;fillColor=none;strokeColor=none;", "", x + 80, 480, 10, 10))
        cells.append(cell(
            f"{p}-ll-{aid}", "1", "endArrow=none;html=1;dashed=1;dashPattern=8 8;strokeColor=#999999;",
            "", edge=True, vertex=False, source=f"{p}-h-{aid}", target=f"{p}-b-{aid}",
        ))
    xmap = {aid: x for aid, _, x in cols}
    msgs = [
        (130, "A", "R", "1. Выдать доступ клиенту"),
        (190, "K", "P", "2. Вход"),
        (250, "K", "P", "3. Создать заявку (без тарифа)"),
        (310, "P", "R", "4. Новая, Source=Client"),
        (370, "A", "R", "5. Назначить тариф и стоимость → Согласована"),
        (430, "K", "P", "6. (опц.) Отменить, пока Новая"),
    ]
    for i, (y, src, tgt, label) in enumerate(msgs):
        cells.append(cell(f"{p}-ms-{i}", "1", "ellipse;fillColor=#ffffff;strokeColor=#666666;", "", xmap[src] + 80, y, 10, 10))
        cells.append(cell(f"{p}-mt-{i}", "1", "ellipse;fillColor=#ffffff;strokeColor=#666666;", "", xmap[tgt] + 80, y, 10, 10))
        cells.append(cell(
            f"{p}-me-{i}", "1", "endArrow=block;html=1;rounded=0;endFill=1;",
            label, edge=True, vertex=False, source=f"{p}-ms-{i}", target=f"{p}-mt-{i}",
        ))
    pages.append(page("7. Последовательность клиента", "page-cseq", cells, 1100, 560))

    # ---- 8. Responsibility zones ----
    p = "zones"
    cells = ['<mxCell id="0" />', '<mxCell id="1" parent="0" />']
    cells.append(cell(f"{p}-title", "1", TITLE, "Роли и зоны ответственности", 40, 20, 500, 30))
    zones = [
        ("front", "Клиентский контур\nКлиент: портал заявок", 40, 80, 220, 120, "#f8cecc", "#b85450"),
        ("mid", "Коммерческий контур\nМенеджер: клиенты, договоры,\nрасчёты, согласование", 300, 80, 260, 120, "#ffe6cc", "#d79b00"),
        ("ops", "Операционный контур\nДиспетчер: заявки, наряды\nБригадир: исполнение", 600, 80, 260, 120, "#dae8fc", "#6c8ebf"),
        ("tech", "Технический контур\nМеханик: автопарк, ТО, ремонт", 40, 280, 260, 100, "#d5e8d4", "#82b366"),
        ("ctrl", "Контроль\nРуководитель: отчёты, просмотр", 340, 280, 240, 100, "#e1d5e7", "#9673a6"),
        ("adm", "Администрирование\nАдминистратор: всё + справочники", 620, 280, 260, 100, "#f5f5f5", "#666666"),
    ]
    for zid, name, x, y, w, h, fill, stroke in zones:
        style = f"rounded=1;whiteSpace=wrap;html=1;fillColor={fill};strokeColor={stroke};fontStyle=1;align=center;"
        cells.append(cell(f"{p}-z-{zid}", "1", style, name, x, y, w, h))
    for i, (s, t) in enumerate([
        ("front", "mid"), ("mid", "ops"), ("ops", "mid"), ("ops", "tech"), ("mid", "ctrl"), ("ops", "ctrl"), ("tech", "ctrl"),
    ]):
        cells.append(cell(f"{p}-e-{i}", "1", EDGE, "", edge=True, vertex=False, source=f"{p}-z-{s}", target=f"{p}-z-{t}"))
    cells.append(cell(
        f"{p}-e-adm", "1", EDGE + "dashed=1;", "включает",
        edge=True, vertex=False, source=f"{p}-z-adm", target=f"{p}-z-mid",
    ))
    pages.append(page("8. Зоны ответственности", "page-zones", cells, 1000, 480))

    # ---- 9. Actors packages ----
    p = "pkg"
    cells = ['<mxCell id="0" />', '<mxCell id="1" parent="0" />']
    cells.append(cell(f"{p}-title", "1", TITLE, "Актёры и пакеты функций", 40, 20, 400, 30))
    packages = [
        ("portal", "Портал клиента\n• Выдача доступа\n• Заявки клиента", 300, 80),
        ("sales", "Клиенты / договоры / расчёты\n• Клиенты и объекты\n• Договоры\n• Расчёты", 520, 80),
        ("ops", "Операции\n• Заявки сотрудников\n• Наряды\n• Рабочая панель", 740, 80),
        ("fleet", "Автопарк\n• Техника\n• ТО и ремонты", 300, 260),
        ("admin", "Администрирование\n• Пользователи\n• Тарифы / бригады / сотрудники", 520, 260),
        ("rep", "Отчёты\n• Сводки и CSV", 740, 260),
    ]
    for pid_, name, x, y in packages:
        cells.append(cell(f"{p}-p-{pid_}", "1", BOX, name, x, y, 200, 140))
    actors_l = [
        ("admin", "Администратор", 40, 80),
        ("manager", "Менеджер", 40, 180),
        ("dispatcher", "Диспетчер", 40, 280),
        ("crew", "Бригадир", 40, 380),
        ("mechanic", "Механик", 40, 480),
        ("director", "Руководитель", 40, 580),
        ("client", "Клиент", 40, 680),
    ]
    for aid, name, x, y in actors_l:
        cells.append(cell(f"{p}-a-{aid}", "1", ACTOR, name, x, y, 40, 70))
    links = [
        ("admin", "portal"), ("admin", "sales"), ("admin", "ops"), ("admin", "fleet"), ("admin", "admin"), ("admin", "rep"),
        ("manager", "portal"), ("manager", "sales"), ("manager", "ops"), ("manager", "rep"),
        ("dispatcher", "ops"), ("dispatcher", "fleet"),
        ("crew", "ops"),
        ("mechanic", "fleet"),
        ("director", "sales"), ("director", "ops"), ("director", "fleet"), ("director", "rep"),
        ("client", "portal"),
    ]
    for i, (a, pkg) in enumerate(links):
        cells.append(cell(
            f"{p}-e-{i}", "1", EDGE_NONE, "",
            edge=True, vertex=False, source=f"{p}-a-{a}", target=f"{p}-p-{pkg}",
        ))
    pages.append(page("9. Актёры и пакеты", "page-pkg", cells, 1100, 800))

    # ---- 10. ER ----
    p = "er"
    cells = ['<mxCell id="0" />', '<mxCell id="1" parent="0" />']
    cells.append(cell(f"{p}-title", "1", TITLE, "Сущности предметной области (упрощённо)", 40, 20, 520, 30))
    ents = [
        ("Client", "Client", 60, 80),
        ("Object", "ServiceObject", 300, 80),
        ("Contract", "Contract", 60, 220),
        ("User", "ApplicationUser", 60, 360),
        ("Request", "ServiceRequest", 540, 80),
        ("Tariff", "Tariff", 780, 80),
        ("Estimate", "CostEstimate", 780, 220),
        ("WO", "WorkOrder", 540, 250),
        ("Crew", "Crew", 300, 250),
        ("Employee", "Employee", 300, 400),
        ("Vehicle", "Vehicle", 540, 400),
        ("Maint", "MaintenanceRecord", 780, 400),
        ("Repair", "RepairRecord", 780, 520),
        ("Report", "WorkReport", 540, 520),
    ]
    for eid, name, x, y in ents:
        cells.append(cell(f"{p}-e-{eid}", "1", "rounded=0;whiteSpace=wrap;html=1;fillColor=#dae8fc;strokeColor=#6c8ebf;fontStyle=1;align=center;", name, x, y, 160, 50))
    rels = [
        ("Client", "Object", "1:*"),
        ("Client", "Contract", "1:*"),
        ("Client", "User", "1:* доступ"),
        ("Object", "Request", "1:*"),
        ("Object", "Employee", "менеджер"),
        ("Object", "Estimate", "1:*"),
        ("Tariff", "Estimate", "1:*"),
        ("Tariff", "Request", "0..1"),
        ("Request", "WO", "0..1"),
        ("WO", "Crew", "N:1"),
        ("WO", "Vehicle", "N:M"),
        ("Crew", "Employee", "1:*"),
        ("Vehicle", "Employee", "водитель"),
        ("Vehicle", "Maint", "1:*"),
        ("Vehicle", "Repair", "1:*"),
        ("WO", "Report", "0..1"),
    ]
    for i, (s, t, label) in enumerate(rels):
        cells.append(cell(
            f"{p}-r-{i}", "1", EDGE_NONE + "endArrow=ERmany;startArrow=ERone;",
            label, edge=True, vertex=False, source=f"{p}-e-{s}", target=f"{p}-e-{t}",
        ))
    pages.append(page("10. Сущности (ER)", "page-er", cells, 1100, 700))

    # ---- 11. Login/Reports note page for thesis figures list ----
    p = "auth"
    cells = ['<mxCell id="0" />', '<mxCell id="1" parent="0" />']
    cells.append(cell(f"{p}-title", "1", TITLE, "Вход / выход и отчёты", 40, 20, 400, 30))
    cells.append(cell(f"{p}-a1", "1", ACTOR, "Пользователь", 80, 120, 40, 80))
    for uid, name, x, y in [
        ("login", "Login", 280, 80),
        ("logout", "Logout", 280, 180),
        ("dash", "Рабочая панель", 280, 280),
        ("rep", "Просмотр отчётов", 500, 120),
        ("csv", "Экспорт CSV", 500, 220),
    ]:
        cells.append(cell(f"{p}-u-{uid}", "1", UC, name, x, y, 160, 60))
    cells.append(cell(f"{p}-sys", "1", BOX, "Система", 250, 50, 460, 320))
    for i, (s, t) in enumerate([
        ("a1", "login"), ("a1", "logout"), ("a1", "dash"), ("a1", "rep"), ("a1", "csv"),
    ]):
        src = f"{p}-{s}" if s == "a1" else f"{p}-u-{s}"
        cells.append(cell(f"{p}-e-{i}", "1", EDGE_NONE, "", edge=True, vertex=False, source=src, target=f"{p}-u-{t}"))
    cells.append(cell(
        f"{p}-note", "1", NOTE,
        "Login: проверка email/пароля Identity → cookie с ролью.\n"
        "Клиент → /Portal; сотрудник → рабочая панель.\n"
        "Logout: удаление cookie.\n"
        "Отчёты: HTML-сводки + CSV (; , UTF-8).",
        80, 420, 420, 120,
    ))
    pages.append(page("11. Login Logout Отчёты", "page-auth", cells, 900, 600))

    header = f'<mxfile host="app.diagrams.net" agent="CleanDistrict-docs" pages="{len(pages)}">'
    return header + "\n" + "\n".join(pages) + "\n</mxfile>\n"


def main() -> None:
    out = Path(__file__).with_name("clean-district-diagrams.drawio")
    out.write_text(build(), encoding="utf-8")
    print(f"Wrote {out} ({out.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
