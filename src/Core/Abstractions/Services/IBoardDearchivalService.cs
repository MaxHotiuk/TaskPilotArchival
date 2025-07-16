using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Models;

namespace Core.Abstractions.Services
{
    public interface IBoardDearchivalService
    {
        Task DearchiveBoardAsync(BoardArchivalMessage message, CancellationToken cancellationToken);
    }
}
