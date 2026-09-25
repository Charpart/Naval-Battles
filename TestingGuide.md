# Гайд по EditMode-тестам Naval Battles

Применяется к тестам в `Assets/NavalBattles/Tests/EditMode`.

## Стек

- Unity Test Framework и NUnit.
- Проверки записываются через NUnit Constraint Model: `Assert.That(actual, Is.EqualTo(expected))`.
- Моки-библиотеки не используются. Время, транспорт и идентичность заменяются небольшими ручными fake.
- Реальное соединение Photon не используется в unit-тестах.

## Что проверять

- Проверяется наблюдаемое поведение на синтетических данных.
- Тесты правил не зависят от ScriptableObject и содержимого сцены.
- Случайная расстановка всегда получает явный seed.
- Сетевые тесты используют управляемые часы и детерминированный транспорт.
- Не фиксируется внешний вид UI; Presenter проверяется через fake View.
- Интеграция Fusion проверяется отдельно от чистой игровой логики.

## Структура

- Namespace теста зеркалит production namespace с префиксом `NavalBattles.Tests.EditMode`.
- Путь теста зеркалит путь production-файла.
- Один `[TestFixture]` соответствует одной тестируемой системе или одному согласованному поведению.
- Общая неизменная настройка находится в `[SetUp]`; сценарные данные создаются отдельными фабричными методами.

## Именование

Тесты называются `MethodName_Scenario_ExpectedResult`:

```csharp
public void TryFire_WhenPlayerIsNotActive_ReturnsWrongTurn()
public void ApplySnapshot_WithOlderRevision_KeepsCurrentState()
public void TryCreateBoard_WithSameSeed_CreatesSameLayout()
```

## Arrange / Act / Assert

Каждый тест содержит явные секции:

```csharp
[Test]
public void TryShoot_WhenCellContainsShip_ReturnsHit()
{
    // Arrange
    BoardState board = CreateBoardWithSingleShip();

    // Act
    bool wasAccepted = board.TryShoot(0, out ShotOutcome outcome);

    // Assert
    Assert.That(wasAccepted, Is.True);
    Assert.That(outcome.result, Is.EqualTo(ShotResult.Hit));
}
```

## Правила тестовых doubles

- `FakeClock` изменяет время только явным вызовом `Advance`.
- `FakeMessageEndpoint` сохраняет копии отправленных байтов и не вызывает receive автоматически.
- `InMemoryNetwork` доставляет сообщения только по явному `Tick`, чтобы тест управлял порядком.
- Fake не содержит игровую логику и не воспроизводит ожидаемый алгоритм production-кода.

## Обязательные группы

- Domain: расстановка, miss/hit/sunk, повторная клетка, ход, timeout, победа.
- Protocol: round-trip и повреждённые/несовместимые сообщения.
- Server: авторизация хода, дедупликация и персональные snapshot.
- Client: pending command, revision и состояние соединения.
- Recovery: loss, duplicate, reorder, reconnect и recreation.

## TDD

Для каждого нового поведения:

1. Написать минимальный тест.
2. Запустить и увидеть ожидаемое падение.
3. Реализовать минимальный код.
4. Запустить целевой тест и весь EditMode-набор.
5. Рефакторить только при зелёных тестах.
