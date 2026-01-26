using Classify.Core.Domain;
using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;

namespace Classify.Data.Repositories;

public class ComposerRepository(ClassifyContext context) : Repository<Composer>(context), IComposerRepository;