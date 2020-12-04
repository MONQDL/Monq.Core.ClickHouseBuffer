﻿using System.Threading.Tasks;

namespace Monq.Core.ClickHouseBuffer
{
    /// <summary>
    /// Интерфейс буфера хранилища событий.
    /// </summary>
    public interface IEventsBufferEngine<T>
        where T : class
    {
        /// <summary>
        /// Добавить событие для записи в ClickHouse.
        /// </summary>
        /// <param name="message">Объект, который требуется записать в ClickHouse.</param>
        /// <returns><see cref="Task"/>, показывающий завершение операции.</returns>
        Task AddEvent(T message);
    }
}
