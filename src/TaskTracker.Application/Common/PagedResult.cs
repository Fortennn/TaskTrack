namespace TaskTracker.Application.Common;

/// <summary>
/// Узагальнений результат запиту з пагінацією.
/// </summary>
/// <typeparam name="T">Тип елементів у списку.</typeparam>
/// <param name="Items">Колекція елементів поточної сторінки.</param>
/// <param name="TotalCount">Загальна кількість елементів, що відповідають фільтрам.</param>
/// <param name="Page">Номер поточної сторінки (починаючи з 1).</param>
/// <param name="PageSize">Кількість елементів на сторінці.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
