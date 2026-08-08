using Classify.Core.Domain;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;

namespace Classify.Data.Repositories;

/// <summary>
/// Implements repository operations for the <see cref="WorkRecording"/> entity.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WorkRecordingRepository"/> class.
/// </remarks>
/// <param name="context">The database context.</param>
public class WorkRecordingRepository(ClassifyContext context) : Repository<WorkRecording>(context), IWorkRecordingRepository
{
}
