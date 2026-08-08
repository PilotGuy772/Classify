using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Services.Queue;

/// <summary>
/// Implements <see cref="INibbleBuilderService"/> to construct <see cref="QueueItem"/> objects using repository data.
/// </summary>
/// <param name="unitOfWork">The database unit of work.</param>
public class NibbleBuilderService(IUnitOfWork unitOfWork) : INibbleBuilderService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    /// <summary>
    /// Constructs a <see cref="QueueItem"/> for a work, choosing its favorite recording or falling back to the first recording by conductor name alphabetically.
    /// </summary>
    /// <param name="workId">The work identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The constructed queue item, or <c>null</c> if the work or a recording is not found.</returns>
    public async Task<QueueItem?> BuildForWorkAsync(int workId, CancellationToken cancellationToken = default)
    {
        Work? work = await _unitOfWork.Works.GetByIdAsync(workId);
        if (work == null)
        {
            return null;
        }

        IEnumerable<Recording> recordings = await _unitOfWork.Recordings.GetRecordingsByWorkIdAsync(workId);
        Recording? recording = SelectRecording(work, recordings);
        if (recording == null)
        {
            return null;
        }

        IEnumerable<Movement> movements = await _unitOfWork.Movements.GetMovementsByWorkIdAsync(workId);
        List<Movement> orderedMovements = OrderMovements(movements);

        Nibble nibble = new()
        {
            WorkId = workId,
            RecordingId = recording.Id
        };

        List<NibbleMovement> nibbleMovements = [];
        int orderIndex = 1;
        foreach (Movement movement in orderedMovements)
        {
            nibbleMovements.Add(new NibbleMovement
            {
                NibbleId = 0,
                MovementId = movement.Id,
                Order = orderIndex++
            });
        }

        return new QueueItem(nibble, nibbleMovements);
    }

    /// <summary>
    /// Constructs a <see cref="QueueItem"/> for a specific recording and all movements of its work ordered.
    /// </summary>
    /// <param name="recordingId">The recording identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The constructed queue item, or <c>null</c> if the recording is not found.</returns>
    public async Task<QueueItem?> BuildForRecordingAsync(int recordingId, CancellationToken cancellationToken = default)
    {
        Recording? recording = await _unitOfWork.Recordings.GetByIdAsync(recordingId);
        if (recording == null)
        {
            return null;
        }

        IEnumerable<Movement> movements = await _unitOfWork.Movements.GetMovementsByWorkIdAsync(recording.WorkId);
        List<Movement> orderedMovements = OrderMovements(movements);

        Nibble nibble = new()
        {
            WorkId = recording.WorkId,
            RecordingId = recording.Id
        };

        List<NibbleMovement> nibbleMovements = [];
        int orderIndex = 1;
        foreach (Movement movement in orderedMovements)
        {
            nibbleMovements.Add(new NibbleMovement
            {
                NibbleId = 0,
                MovementId = movement.Id,
                Order = orderIndex++
            });
        }

        return new QueueItem(nibble, nibbleMovements);
    }

    /// <summary>
    /// Constructs a <see cref="QueueItem"/> for a single movement, resolving a specified recording or falling back to the work's favorite/alphabetical recording.
    /// </summary>
    /// <param name="movementId">The movement identifier.</param>
    /// <param name="recordingId">Optional explicit recording identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The constructed queue item, or <c>null</c> if the movement or recording is not found.</returns>
    public async Task<QueueItem?> BuildForMovementAsync(int movementId, int? recordingId = null, CancellationToken cancellationToken = default)
    {
        Movement? movement = await _unitOfWork.Movements.GetByIdAsync(movementId);
        if (movement == null)
        {
            return null;
        }

        Recording? recording = null;
        if (recordingId.HasValue)
        {
            recording = await _unitOfWork.Recordings.GetByIdAsync(recordingId.Value);
        }

        if (recording == null)
        {
            Work? work = await _unitOfWork.Works.GetByIdAsync(movement.WorkId);
            if (work != null)
            {
                IEnumerable<Recording> recordings = await _unitOfWork.Recordings.GetRecordingsByWorkIdAsync(work.Id);
                recording = SelectRecording(work, recordings);
            }
        }

        if (recording == null)
        {
            return null;
        }

        Nibble nibble = new()
        {
            WorkId = movement.WorkId,
            RecordingId = recording.Id
        };

        List<NibbleMovement> nibbleMovements =
        [
            new NibbleMovement
            {
                NibbleId = 0,
                MovementId = movement.Id,
                Order = movement.Order
            }
        ];

        return new QueueItem(nibble, nibbleMovements);
    }

    /// <summary>
    /// Selects the recording for a work: returns the work's favorite recording if available, or falls back to the first recording by conductor name alphabetically.
    /// </summary>
    /// <param name="work">The work entity.</param>
    /// <param name="recordings">The collection of recordings available for the work.</param>
    /// <returns>The selected recording, or <c>null</c> if no recordings exist.</returns>
    private static Recording? SelectRecording(Work work, IEnumerable<Recording> recordings)
    {
        List<Recording> list = recordings.ToList();
        if (list.Count == 0)
        {
            return null;
        }

        if (work.FavoriteRecordingId.HasValue)
        {
            Recording? favorite = list.FirstOrDefault(r => r.Id == work.FavoriteRecordingId.Value);
            if (favorite != null)
            {
                return favorite;
            }
        }

        return list
            .OrderBy(r => string.IsNullOrWhiteSpace(r.Conductor) ? "ZZZZZZ" : r.Conductor, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// Orders movements by their <see cref="Movement.Order"/> property, then by name.
    /// </summary>
    /// <param name="movements">The movements to order.</param>
    /// <returns>The ordered list of movements.</returns>
    private static List<Movement> OrderMovements(IEnumerable<Movement> movements)
    {
        List<Movement> list = movements.ToList();
        list.Sort((Movement a, Movement b) =>
        {
            int orderCompare = a.Order.CompareTo(b.Order);
            if (orderCompare != 0)
            {
                return orderCompare;
            }
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });
        return list;
    }
}
