using Classify.Core.Domain;
using Classify.Core.Enums;
using Classify.Core.Interfaces.Infrastructure;

namespace Classify.Data.Seeders;

/// <summary>
/// Seeds the database with a realistic classical music library containing famous composers, works, movements, and recordings.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DemoLibrarySeeder"/> class.
/// </remarks>
/// <param name="uow">The unit of work for database access.</param>
public sealed class DemoLibrarySeeder(IUnitOfWork uow) : IDatabaseSeeder
{
    /// <summary>
    /// The current audio file count used for generating paths and hashes.
    /// </summary>
    private int _audioFileCount = 1;

    /// <summary>
    /// Represents template data for a movement.
    /// </summary>
    private sealed class MovementTemplate
    {
        /// <summary>
        /// Gets or sets the name of the movement.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the order/index of the movement within the work.
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Represents template data for a work.
    /// </summary>
    private sealed class WorkTemplate
    {
        /// <summary>
        /// Gets or sets the name of the work.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the catalog number of the work.
        /// </summary>
        public string CatalogNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the movements of the work.
        /// </summary>
        public MovementTemplate[] Movements { get; set; } = Array.Empty<MovementTemplate>();
    }

    /// <summary>
    /// Represents template data for a composer.
    /// </summary>
    private sealed class ComposerTemplate
    {
        /// <summary>
        /// Gets or sets the name of the composer.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the works of the composer.
        /// </summary>
        public WorkTemplate[] Works { get; set; } = Array.Empty<WorkTemplate>();
    }

    /// <summary>
    /// Seeds the database with classical music data if no composers currently exist.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await uow.Composers.AnyAsync())
        {
            return;
        }

        ComposerTemplate[] templates = GetComposerTemplates();

        // Keep track of added entities to associate with recordings/movements
        List<Composer> addedComposers = new List<Composer>();
        List<Work> addedWorks = new List<Work>();
        List<Movement> addedMovements = new List<Movement>();

        int workOffset = 0;

        foreach (ComposerTemplate composerTemplate in templates)
        {
            Composer composer = new Composer
            {
                Name = composerTemplate.Name
            };
            await uow.Composers.AddAsync(composer);
            await uow.SaveChangesAsync();
            addedComposers.Add(composer);

            foreach (WorkTemplate workTemplate in composerTemplate.Works)
            {
                Work work = new Work
                {
                    ComposerId = composer.Id,
                    CatalogNumber = workTemplate.CatalogNumber,
                    Name = workTemplate.Name
                };
                await uow.Works.AddAsync(work);
                await uow.SaveChangesAsync();
                addedWorks.Add(work);

                List<Movement> workMovements = new List<Movement>();
                foreach (MovementTemplate movementTemplate in workTemplate.Movements)
                {
                    Movement movement = new Movement
                    {
                        WorkId = work.Id,
                        Name = movementTemplate.Name,
                        Order = movementTemplate.Order
                    };
                    await uow.Movements.AddAsync(movement);
                    workMovements.Add(movement);
                }
                await uow.SaveChangesAsync();
                addedMovements.AddRange(workMovements);

                // Create a standard primary recording for this work with realistic variety
                Recording recording = CreateRealisticRecording(composer.Name, workTemplate, work.Id, workOffset++);
                await uow.Recordings.AddAsync(recording);
                await uow.SaveChangesAsync();

                // Add performed movements and audio files for this standard recording
                foreach (Movement movement in workMovements)
                {
                    AudioFile audioFile = new AudioFile
                    {
                        Path = $"/music/{composer.Name.Replace(" ", "_")}/{work.Name.Replace(" ", "_")}/track_{_audioFileCount++:D3}.flac",
                        Hash = (ulong)_audioFileCount * 123456789UL,
                        Status = IngestionStatus.Complete
                    };
                    await uow.AudioFiles.AddAsync(audioFile);
                    await uow.SaveChangesAsync();

                    PerformedMovement performedMovement = new PerformedMovement
                    {
                        RecordingId = recording.Id,
                        MovementId = movement.Id,
                        AudioFileId = audioFile.Id,
                        Order = movement.Order
                    };
                    await uow.PerformedMovements.AddAsync(performedMovement);
                }
                await uow.SaveChangesAsync();
            }
        }

        // Add additional recordings for specific works to satisfy "ensure at least some works have more than one recording"
        await AddAlternativeRecordingsAsync(addedWorks, addedMovements);

        // Add overlapping recordings that span multiple works
        await AddOverlappingRecordingsAsync(addedWorks, addedMovements);

        await uow.SaveChangesAsync();
    }

    /// <summary>
    /// Adds alternative recordings for some of the seeded works.
    /// </summary>
    private async Task AddAlternativeRecordingsAsync(List<Work> works, List<Movement> movements)
    {
        // 1. Beethoven's Symphony No. 5
        Work? beethovenSymphony5 = works.FirstOrDefault(w => w.Name == "Symphony No. 5 in C minor");
        if (beethovenSymphony5 != null)
        {
            Recording altRecording = new Recording
            {
                WorkId = beethovenSymphony5.Id,
                Conductor = "Carlos Kleiber",
                Ensemble = "Vienna Philharmonic Orchestra",
                Year = 1975
            };
            await uow.Recordings.AddAsync(altRecording);
            await uow.SaveChangesAsync();

            List<Movement> s5Movements = movements.Where(m => m.WorkId == beethovenSymphony5.Id).OrderBy(m => m.Order).ToList();
            foreach (Movement movement in s5Movements)
            {
                AudioFile audioFile = new AudioFile
                {
                    Path = $"/music/Beethoven/Symphony_5_Kleiber/track_{_audioFileCount++:D3}.flac",
                    Hash = (ulong)_audioFileCount * 987654321UL,
                    Status = IngestionStatus.Complete
                };
                await uow.AudioFiles.AddAsync(audioFile);
                await uow.SaveChangesAsync();

                PerformedMovement performedMovement = new PerformedMovement
                {
                    RecordingId = altRecording.Id,
                    MovementId = movement.Id,
                    AudioFileId = audioFile.Id,
                    Order = movement.Order
                };
                await uow.PerformedMovements.AddAsync(performedMovement);
            }
        }

        // 2. Bach's Goldberg Variations
        Work? goldbergVariations = works.FirstOrDefault(w => w.Name == "Goldberg Variations");
        if (goldbergVariations != null)
        {
            Recording altRecording = new Recording
            {
                WorkId = goldbergVariations.Id,
                Soloist = "Glenn Gould",
                Year = 1955
            };
            await uow.Recordings.AddAsync(altRecording);
            await uow.SaveChangesAsync();

            List<Movement> gvMovements = movements.Where(m => m.WorkId == goldbergVariations.Id).OrderBy(m => m.Order).ToList();
            foreach (Movement movement in gvMovements)
            {
                AudioFile audioFile = new AudioFile
                {
                    Path = $"/music/Bach/Goldberg_Gould_1955/track_{_audioFileCount++:D3}.flac",
                    Hash = (ulong)_audioFileCount * 987654321UL,
                    Status = IngestionStatus.Complete
                };
                await uow.AudioFiles.AddAsync(audioFile);
                await uow.SaveChangesAsync();

                PerformedMovement performedMovement = new PerformedMovement
                {
                    RecordingId = altRecording.Id,
                    MovementId = movement.Id,
                    AudioFileId = audioFile.Id,
                    Order = movement.Order
                };
                await uow.PerformedMovements.AddAsync(performedMovement);
            }
        }
    }

    /// <summary>
    /// Adds overlapping recordings that span multiple works.
    /// </summary>
    private async Task AddOverlappingRecordingsAsync(List<Work> works, List<Movement> movements)
    {
        // Overlap 1: "The Four Seasons" - A single compilation recording spanning Spring, Summer, Autumn, and Winter
        Work? spring = works.FirstOrDefault(w => w.Name == "The Four Seasons - Spring");
        Work? summer = works.FirstOrDefault(w => w.Name == "The Four Seasons - Summer");
        Work? autumn = works.FirstOrDefault(w => w.Name == "The Four Seasons - Autumn");
        Work? winter = works.FirstOrDefault(w => w.Name == "The Four Seasons - Winter");

        if (spring != null && summer != null && autumn != null && winter != null)
        {
            // Associate the recording with "Spring" as the main work
            Recording seasonsRecording = new Recording
            {
                WorkId = spring.Id,
                Conductor = "Neville Marriner",
                Ensemble = "Academy of St Martin in the Fields",
                Soloist = "Alan Loveday",
                Year = 1969
            };
            await uow.Recordings.AddAsync(seasonsRecording);
            await uow.SaveChangesAsync();

            List<Work> seasonsWorks = new List<Work> { spring, summer, autumn, winter };
            int orderCounter = 1;
            foreach (Work work in seasonsWorks)
            {
                List<Movement> workMovements = movements.Where(m => m.WorkId == work.Id).OrderBy(m => m.Order).ToList();
                foreach (Movement movement in workMovements)
                {
                    AudioFile audioFile = new AudioFile
                    {
                        Path = $"/music/Vivaldi/Four_Seasons_Marriner/track_{_audioFileCount++:D3}.flac",
                        Hash = (ulong)_audioFileCount * 1122334455UL,
                        Status = IngestionStatus.Complete
                    };
                    await uow.AudioFiles.AddAsync(audioFile);
                    await uow.SaveChangesAsync();

                    PerformedMovement performedMovement = new PerformedMovement
                    {
                        RecordingId = seasonsRecording.Id,
                        MovementId = movement.Id,
                        AudioFileId = audioFile.Id,
                        Order = orderCounter++
                    };
                    await uow.PerformedMovements.AddAsync(performedMovement);
                }
            }
        }

        // Overlap 2: "Beethoven & Brahms: Violin Concertos" - Spanning Beethoven Violin Concerto and Brahms Violin Concerto
        Work? beethovenViolin = works.FirstOrDefault(w => w.Name == "Violin Concerto in D major" && w.CatalogNumber == "Op. 61");
        Work? brahmsViolin = works.FirstOrDefault(w => w.Name == "Violin Concerto in D major" && w.CatalogNumber == "Op. 77");

        if (beethovenViolin != null && brahmsViolin != null)
        {
            Recording violinConcertosRecording = new Recording
            {
                WorkId = beethovenViolin.Id,
                Conductor = "Jascha Heifetz",
                Ensemble = "Boston Symphony Orchestra",
                Year = 1955
            };
            await uow.Recordings.AddAsync(violinConcertosRecording);
            await uow.SaveChangesAsync();

            List<Work> concertosWorks = new List<Work> { beethovenViolin, brahmsViolin };
            int orderCounter = 1;
            foreach (Work work in concertosWorks)
            {
                List<Movement> workMovements = movements.Where(m => m.WorkId == work.Id).OrderBy(m => m.Order).ToList();
                foreach (Movement movement in workMovements)
                {
                    AudioFile audioFile = new AudioFile
                    {
                        Path = $"/music/Concertos/Heifetz_Violin_Concertos/track_{_audioFileCount++:D3}.flac",
                        Hash = (ulong)_audioFileCount * 9988776655UL,
                        Status = IngestionStatus.Complete
                    };
                    await uow.AudioFiles.AddAsync(audioFile);
                    await uow.SaveChangesAsync();

                    PerformedMovement performedMovement = new PerformedMovement
                    {
                        RecordingId = violinConcertosRecording.Id,
                        MovementId = movement.Id,
                        AudioFileId = audioFile.Id,
                        Order = orderCounter++
                    };
                    await uow.PerformedMovements.AddAsync(performedMovement);
                }
            }
        }
    }

    /// <summary>
    /// Generates a realistic recording for a given composer and work template.
    /// </summary>
    private static Recording CreateRealisticRecording(string composerName, WorkTemplate workTemplate, int workId, int seedOffset)
    {
        string conductor = string.Empty;
        string? ensemble = null;
        string? soloist = null;
        int year = 1970 + (seedOffset % 45);

        string workNameLower = workTemplate.Name.ToLower();

        if (workNameLower.Contains("quartet") || workNameLower.Contains("quintet") || workNameLower.Contains("octet"))
        {
            string[] ensembles = new string[] { "Alban Berg Quartett", "Emerson String Quartet", "Takács Quartet", "Juilliard String Quartet", "Borodin Quartet" };
            ensemble = ensembles[seedOffset % ensembles.Length];
            year = 1980 + (seedOffset % 30);
        }
        else if (workNameLower.Contains("requiem") || workNameLower.Contains("passion") || workNameLower.Contains("mass") || 
                 workNameLower.Contains("traviata") || workNameLower.Contains("aida") || workNameLower.Contains("rigoletto") || 
                 workNameLower.Contains("otello") || workNameLower.Contains("opera") || workNameLower.Contains("messiah") || 
                 workNameLower.Contains("gloria") || workNameLower.Contains("rusalka"))
        {
            string[] conductors = new string[] { "John Eliot Gardiner", "Karl Richter", "Georg Solti", "Herbert von Karajan", "Claudio Abbado" };
            string[] ensembles = new string[] { "English Baroque Soloists & Monteverdi Choir", "Munich Bach Orchestra", "Vienna State Opera Orchestra", "Berlin Philharmonic Orchestra", "London Symphony Chorus & Orchestra" };
            conductor = conductors[seedOffset % conductors.Length];
            ensemble = ensembles[seedOffset % ensembles.Length];
            year = 1970 + (seedOffset % 35);
        }
        else if (workNameLower.Contains("concerto") || workNameLower.Contains("rhapsody on a theme"))
        {
            string[] conductors = new string[] { "Leonard Bernstein", "Bernard Haitink", "Claudio Abbado", "Georg Solti", "Zubin Mehta" };
            string[] ensembles = new string[] { "Chicago Symphony Orchestra", "Vienna Philharmonic Orchestra", "London Symphony Orchestra", "Royal Concertgebouw Orchestra", "Boston Symphony Orchestra" };
            
            conductor = conductors[seedOffset % conductors.Length];
            ensemble = ensembles[seedOffset % ensembles.Length];

            if (workNameLower.Contains("violin"))
            {
                string[] soloists = new string[] { "Itzhak Perlman", "Anne-Sophie Mutter", "Jascha Heifetz", "David Oistrakh", "Joshua Bell" };
                soloist = soloists[seedOffset % soloists.Length];
            }
            else if (workNameLower.Contains("cello"))
            {
                string[] soloists = new string[] { "Mstislav Rostropovich", "Jacqueline du Pré", "Yo-Yo Ma", "Jian Wang" };
                soloist = soloists[seedOffset % soloists.Length];
            }
            else
            {
                string[] soloists = new string[] { "Martha Argerich", "Vladimir Ashkenazy", "Krystian Zimerman", "Alfred Brendel", "Evgeny Kissin" };
                soloist = soloists[seedOffset % soloists.Length];
            }
            year = 1970 + (seedOffset % 40);
        }
        else if (workNameLower.Contains("variations") || workNameLower.Contains("sonata") || workNameLower.Contains("nocturne") || 
                 workNameLower.Contains("ballade") || workNameLower.Contains("prelude") || workNameLower.Contains("fantaisie") || 
                 workNameLower.Contains("liebesträume") || workNameLower.Contains("kinderszenen") || workNameLower.Contains("gaspard") ||
                 workNameLower.Contains("boléro") || workNameLower.Contains("pavane"))
        {
            if (composerName == "Johann Sebastian Bach" && workNameLower.Contains("cello"))
            {
                string[] soloists = new string[] { "Mstislav Rostropovich", "Yo-Yo Ma", "Pierre Fournier", "Pablo Casals" };
                soloist = soloists[seedOffset % soloists.Length];
            }
            else
            {
                string[] soloists = new string[] { "Vladimir Horowitz", "Arthur Rubinstein", "Glenn Gould", "Sviatoslav Richter", "Martha Argerich", "Maurizio Pollini", "Claudio Arrau" };
                soloist = soloists[seedOffset % soloists.Length];
            }
            year = 1960 + (seedOffset % 45);
        }
        else
        {
            string[] conductors = new string[] { "Herbert von Karajan", "Leonard Bernstein", "Carlos Kleiber", "Pierre Boulez", "Simon Rattle", "Valery Gergiev", "Neville Marriner" };
            string[] ensembles = new string[] { "Berlin Philharmonic Orchestra", "Vienna Philharmonic Orchestra", "Chicago Symphony Orchestra", "London Symphony Orchestra", "Cleveland Orchestra", "New York Philharmonic" };
            conductor = conductors[seedOffset % conductors.Length];
            ensemble = ensembles[seedOffset % ensembles.Length];
            year = 1970 + (seedOffset % 45);
        }

        return new Recording
        {
            WorkId = workId,
            Conductor = conductor,
            Ensemble = ensemble,
            Soloist = soloist,
            Year = year
        };
    }

    /// <summary>
    /// Gets the list of 20 composers and their 100 works.
    /// </summary>
    /// <returns>An array of composer templates.</returns>
    private static ComposerTemplate[] GetComposerTemplates()
    {
        return new ComposerTemplate[]
        {
            new ComposerTemplate
            {
                Name = "Johann Sebastian Bach",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "St Matthew Passion", CatalogNumber = "BWV 244", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Part I", Order = 1 }, new MovementTemplate { Name = "Part II", Order = 2 } } },
                    new WorkTemplate { Name = "Goldberg Variations", CatalogNumber = "BWV 988", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Aria", Order = 1 }, new MovementTemplate { Name = "Variations", Order = 2 }, new MovementTemplate { Name = "Aria da Capo", Order = 3 } } },
                    new WorkTemplate { Name = "Cello Suite No. 1 in G major", CatalogNumber = "BWV 1007", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Prelude", Order = 1 }, new MovementTemplate { Name = "Allemande", Order = 2 }, new MovementTemplate { Name = "Courante", Order = 3 }, new MovementTemplate { Name = "Sarabande", Order = 4 }, new MovementTemplate { Name = "Minuets", Order = 5 }, new MovementTemplate { Name = "Gigue", Order = 6 } } },
                    new WorkTemplate { Name = "Brandenburg Concerto No. 3 in G major", CatalogNumber = "BWV 1048", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro", Order = 1 }, new MovementTemplate { Name = "Adagio", Order = 2 }, new MovementTemplate { Name = "Allegro", Order = 3 } } },
                    new WorkTemplate { Name = "Mass in B minor", CatalogNumber = "BWV 232", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Kyrie", Order = 1 }, new MovementTemplate { Name = "Gloria", Order = 2 }, new MovementTemplate { Name = "Credo", Order = 3 }, new MovementTemplate { Name = "Sanctus", Order = 4 }, new MovementTemplate { Name = "Agnus Dei", Order = 5 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Wolfgang Amadeus Mozart",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Symphony No. 41 in C major 'Jupiter'", CatalogNumber = "K. 551", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro vivace", Order = 1 }, new MovementTemplate { Name = "Andante cantabile", Order = 2 }, new MovementTemplate { Name = "Menuetto", Order = 3 }, new MovementTemplate { Name = "Molto allegro", Order = 4 } } },
                    new WorkTemplate { Name = "Piano Concerto No. 21 in C major", CatalogNumber = "K. 467", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro maestoso", Order = 1 }, new MovementTemplate { Name = "Andante", Order = 2 }, new MovementTemplate { Name = "Allegro vivace assai", Order = 3 } } },
                    new WorkTemplate { Name = "Requiem in D minor", CatalogNumber = "K. 626", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Introitus", Order = 1 }, new MovementTemplate { Name = "Kyrie", Order = 2 }, new MovementTemplate { Name = "Sequentia", Order = 3 }, new MovementTemplate { Name = "Offertorium", Order = 4 }, new MovementTemplate { Name = "Sanctus", Order = 5 }, new MovementTemplate { Name = "Benedictus", Order = 6 }, new MovementTemplate { Name = "Agnus Dei", Order = 7 }, new MovementTemplate { Name = "Communio", Order = 8 } } },
                    new WorkTemplate { Name = "The Marriage of Figaro", CatalogNumber = "K. 492", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Overture", Order = 1 }, new MovementTemplate { Name = "Act I", Order = 2 }, new MovementTemplate { Name = "Act II", Order = 3 }, new MovementTemplate { Name = "Act III", Order = 4 }, new MovementTemplate { Name = "Act IV", Order = 5 } } },
                    new WorkTemplate { Name = "Clarinet Concerto in A major", CatalogNumber = "K. 622", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro", Order = 1 }, new MovementTemplate { Name = "Adagio", Order = 2 }, new MovementTemplate { Name = "Rondo", Order = 3 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Ludwig van Beethoven",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Symphony No. 5 in C minor", CatalogNumber = "Op. 67", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro con brio", Order = 1 }, new MovementTemplate { Name = "Andante con moto", Order = 2 }, new MovementTemplate { Name = "Scherzo", Order = 3 }, new MovementTemplate { Name = "Allegro", Order = 4 } } },
                    new WorkTemplate { Name = "Symphony No. 9 in D minor", CatalogNumber = "Op. 125", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro ma non troppo", Order = 1 }, new MovementTemplate { Name = "Molto vivace", Order = 2 }, new MovementTemplate { Name = "Adagio molto", Order = 3 }, new MovementTemplate { Name = "Finale", Order = 4 } } },
                    new WorkTemplate { Name = "Piano Sonata No. 14 'Moonlight'", CatalogNumber = "Op. 27 No. 2", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Adagio sostenuto", Order = 1 }, new MovementTemplate { Name = "Allegretto", Order = 2 }, new MovementTemplate { Name = "Presto agitato", Order = 3 } } },
                    new WorkTemplate { Name = "Violin Concerto in D major", CatalogNumber = "Op. 61", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro ma non troppo", Order = 1 }, new MovementTemplate { Name = "Larghetto", Order = 2 }, new MovementTemplate { Name = "Rondo", Order = 3 } } },
                    new WorkTemplate { Name = "Piano Concerto No. 5 'Emperor'", CatalogNumber = "Op. 73", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro", Order = 1 }, new MovementTemplate { Name = "Adagio un poco mosso", Order = 2 }, new MovementTemplate { Name = "Rondo", Order = 3 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Franz Schubert",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Symphony No. 8 in B minor 'Unfinished'", CatalogNumber = "D. 759", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro moderato", Order = 1 }, new MovementTemplate { Name = "Andante con moto", Order = 2 } } },
                    new WorkTemplate { Name = "Winterreise", CatalogNumber = "D. 911", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Gute Nacht", Order = 1 }, new MovementTemplate { Name = "Der Lindenbaum", Order = 2 }, new MovementTemplate { Name = "Die Post", Order = 3 }, new MovementTemplate { Name = "Der Leiermann", Order = 4 } } },
                    new WorkTemplate { Name = "String Quintet in C major", CatalogNumber = "D. 956", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro ma non troppo", Order = 1 }, new MovementTemplate { Name = "Adagio", Order = 2 }, new MovementTemplate { Name = "Scherzo", Order = 3 }, new MovementTemplate { Name = "Allegretto", Order = 4 } } },
                    new WorkTemplate { Name = "Piano Quintet in A major 'Trout'", CatalogNumber = "D. 667", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro vivace", Order = 1 }, new MovementTemplate { Name = "Andante", Order = 2 }, new MovementTemplate { Name = "Scherzo", Order = 3 }, new MovementTemplate { Name = "Tema con variazioni", Order = 4 }, new MovementTemplate { Name = "Finale", Order = 5 } } },
                    new WorkTemplate { Name = "Ave Maria", CatalogNumber = "D. 839", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Ave Maria", Order = 1 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Frédéric Chopin",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Nocturnes, Op. 9", CatalogNumber = "Op. 9", Movements = new MovementTemplate[] { new MovementTemplate { Name = "No. 1 in B-flat minor", Order = 1 }, new MovementTemplate { Name = "No. 2 in E-flat major", Order = 2 }, new MovementTemplate { Name = "No. 3 in B major", Order = 3 } } },
                    new WorkTemplate { Name = "Ballade No. 1 in G minor", CatalogNumber = "Op. 23", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Ballade", Order = 1 } } },
                    new WorkTemplate { Name = "Preludes, Op. 28", CatalogNumber = "Op. 28", Movements = new MovementTemplate[] { new MovementTemplate { Name = "No. 4 in E minor", Order = 1 }, new MovementTemplate { Name = "No. 15 'Raindrop'", Order = 2 }, new MovementTemplate { Name = "No. 20 in C minor", Order = 3 } } },
                    new WorkTemplate { Name = "Piano Sonata No. 2 in B-flat minor", CatalogNumber = "Op. 35", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Grave", Order = 1 }, new MovementTemplate { Name = "Scherzo", Order = 2 }, new MovementTemplate { Name = "Marche funèbre", Order = 3 }, new MovementTemplate { Name = "Presto", Order = 4 } } },
                    new WorkTemplate { Name = "Fantaisie-Impromptu", CatalogNumber = "Op. 66", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Moderato cantabile", Order = 1 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Johannes Brahms",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Symphony No. 1 in C minor", CatalogNumber = "Op. 68", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Un poco sostenuto", Order = 1 }, new MovementTemplate { Name = "Andante sostenuto", Order = 2 }, new MovementTemplate { Name = "Un poco allegretto", Order = 3 }, new MovementTemplate { Name = "Adagio", Order = 4 } } },
                    new WorkTemplate { Name = "Violin Concerto in D major", CatalogNumber = "Op. 77", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro non troppo", Order = 1 }, new MovementTemplate { Name = "Adagio", Order = 2 }, new MovementTemplate { Name = "Allegro giocoso", Order = 3 } } },
                    new WorkTemplate { Name = "A German Requiem", CatalogNumber = "Op. 45", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Selig sind", Order = 1 }, new MovementTemplate { Name = "Denn alles Fleisch", Order = 2 }, new MovementTemplate { Name = "Herr, lehre doch mich", Order = 3 } } },
                    new WorkTemplate { Name = "Piano Concerto No. 2 in B-flat major", CatalogNumber = "Op. 83", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro non troppo", Order = 1 }, new MovementTemplate { Name = "Allegro appassionato", Order = 2 }, new MovementTemplate { Name = "Andante", Order = 3 }, new MovementTemplate { Name = "Allegretto grazioso", Order = 4 } } },
                    new WorkTemplate { Name = "Hungarian Dances", CatalogNumber = "WoO 1", Movements = new MovementTemplate[] { new MovementTemplate { Name = "No. 1 in G minor", Order = 1 }, new MovementTemplate { Name = "No. 5 in F-sharp minor", Order = 2 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Pyotr Ilyich Tchaikovsky",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Symphony No. 6 in B minor 'Pathétique'", CatalogNumber = "Op. 74", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Adagio", Order = 1 }, new MovementTemplate { Name = "Allegro con grazia", Order = 2 }, new MovementTemplate { Name = "Allegro molto vivace", Order = 3 }, new MovementTemplate { Name = "Finale", Order = 4 } } },
                    new WorkTemplate { Name = "The Nutcracker", CatalogNumber = "Op. 71", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Overture", Order = 1 }, new MovementTemplate { Name = "March", Order = 2 }, new MovementTemplate { Name = "Dance of the Sugar Plum Fairy", Order = 3 }, new MovementTemplate { Name = "Russian Dance", Order = 4 }, new MovementTemplate { Name = "Waltz of the Flowers", Order = 5 } } },
                    new WorkTemplate { Name = "Swan Lake", CatalogNumber = "Op. 20", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Scene", Order = 1 }, new MovementTemplate { Name = "Waltz", Order = 2 }, new MovementTemplate { Name = "Dance of the Swans", Order = 3 }, new MovementTemplate { Name = "Hungarian Dance", Order = 4 } } },
                    new WorkTemplate { Name = "Violin Concerto in D major", CatalogNumber = "Op. 35", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro moderato", Order = 1 }, new MovementTemplate { Name = "Canzonetta", Order = 2 }, new MovementTemplate { Name = "Finale", Order = 3 } } },
                    new WorkTemplate { Name = "Piano Concerto No. 1 in B-flat minor", CatalogNumber = "Op. 23", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro non troppo", Order = 1 }, new MovementTemplate { Name = "Andantino simplice", Order = 2 }, new MovementTemplate { Name = "Allegro con fuoco", Order = 3 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Claude Debussy",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "La Mer", CatalogNumber = "L. 111", Movements = new MovementTemplate[] { new MovementTemplate { Name = "De l'aube à midi sur la mer", Order = 1 }, new MovementTemplate { Name = "Jeux de vagues", Order = 2 }, new MovementTemplate { Name = "Dialogue du vent et de la mer", Order = 3 } } },
                    new WorkTemplate { Name = "Prélude à l'après-midi d'un faune", CatalogNumber = "L. 87", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Prélude", Order = 1 } } },
                    new WorkTemplate { Name = "Suite bergamasque", CatalogNumber = "L. 75", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Prélude", Order = 1 }, new MovementTemplate { Name = "Menuet", Order = 2 }, new MovementTemplate { Name = "Clair de lune", Order = 3 }, new MovementTemplate { Name = "Passepied", Order = 4 } } },
                    new WorkTemplate { Name = "Nocturnes", CatalogNumber = "L. 91", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Nuages", Order = 1 }, new MovementTemplate { Name = "Fêtes", Order = 2 }, new MovementTemplate { Name = "Sirènes", Order = 3 } } },
                    new WorkTemplate { Name = "String Quartet in G minor", CatalogNumber = "Op. 10", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Animé et très décidé", Order = 1 }, new MovementTemplate { Name = "Assez vif", Order = 2 }, new MovementTemplate { Name = "Andantino doucement expressif", Order = 3 }, new MovementTemplate { Name = "Très modéré", Order = 4 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Igor Stravinsky",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "The Rite of Spring", CatalogNumber = "K015", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Part I: Adoration of the Earth", Order = 1 }, new MovementTemplate { Name = "Part II: The Sacrifice", Order = 2 } } },
                    new WorkTemplate { Name = "The Firebird", CatalogNumber = "K010", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Introduction", Order = 1 }, new MovementTemplate { Name = "Dance of the Firebird", Order = 2 }, new MovementTemplate { Name = "Infernal Dance", Order = 3 }, new MovementTemplate { Name = "Lullaby", Order = 4 }, new MovementTemplate { Name = "Finale", Order = 5 } } },
                    new WorkTemplate { Name = "Petrushka", CatalogNumber = "K012", Movements = new MovementTemplate[] { new MovementTemplate { Name = "The Shrovetide Fair", Order = 1 }, new MovementTemplate { Name = "Petrushka's Room", Order = 2 }, new MovementTemplate { Name = "The Moor's Room", Order = 3 }, new MovementTemplate { Name = "The Shrovetide Fair - Evening", Order = 4 } } },
                    new WorkTemplate { Name = "Symphony of Psalms", CatalogNumber = "K053", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Part I", Order = 1 }, new MovementTemplate { Name = "Part II", Order = 2 }, new MovementTemplate { Name = "Part III", Order = 3 } } },
                    new WorkTemplate { Name = "L'Histoire du soldat", CatalogNumber = "K029", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Soldier's March", Order = 1 }, new MovementTemplate { Name = "Music to Scene 1", Order = 2 }, new MovementTemplate { Name = "Music to Scene 2", Order = 3 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Giuseppe Verdi",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Messa da Requiem", CatalogNumber = "Verdi Requiem", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Requiem aeternam", Order = 1 }, new MovementTemplate { Name = "Dies irae", Order = 2 }, new MovementTemplate { Name = "Offertorio", Order = 3 }, new MovementTemplate { Name = "Sanctus", Order = 4 }, new MovementTemplate { Name = "Agnus Dei", Order = 5 } } },
                    new WorkTemplate { Name = "La Traviata", CatalogNumber = "Opera", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Overture", Order = 1 }, new MovementTemplate { Name = "Libiamo ne' lieti calici", Order = 2 }, new MovementTemplate { Name = "Sempre libera", Order = 3 } } },
                    new WorkTemplate { Name = "Aida", CatalogNumber = "Opera", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Prelude", Order = 1 }, new MovementTemplate { Name = "Celeste Aida", Order = 2 }, new MovementTemplate { Name = "Grand March", Order = 3 } } },
                    new WorkTemplate { Name = "Rigoletto", CatalogNumber = "Opera", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Questa o quella", Order = 1 }, new MovementTemplate { Name = "Caro nome", Order = 2 }, new MovementTemplate { Name = "La donna è mobile", Order = 3 } } },
                    new WorkTemplate { Name = "Otello", CatalogNumber = "Opera", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Esultate", Order = 1 }, new MovementTemplate { Name = "Gia nella notte", Order = 2 }, new MovementTemplate { Name = "Ave Maria", Order = 3 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Richard Wagner",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Tristan und Isolde", CatalogNumber = "WWV 90", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Prelude", Order = 1 }, new MovementTemplate { Name = "Mild und leise (Liebestod)", Order = 2 } } },
                    new WorkTemplate { Name = "Die Walküre", CatalogNumber = "WWV 86B", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Ride of the Valkyries", Order = 1 }, new MovementTemplate { Name = "Wotan's Farewell", Order = 2 } } },
                    new WorkTemplate { Name = "Tannhäuser", CatalogNumber = "WWV 70", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Overture", Order = 1 }, new MovementTemplate { Name = "Pilgrim's Chorus", Order = 2 } } },
                    new WorkTemplate { Name = "Lohengrin", CatalogNumber = "WWV 75", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Prelude to Act I", Order = 1 }, new MovementTemplate { Name = "Bridal Chorus", Order = 2 }, new MovementTemplate { Name = "Prelude to Act III", Order = 3 } } },
                    new WorkTemplate { Name = "Der Fliegende Holländer", CatalogNumber = "WWV 63", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Overture", Order = 1 }, new MovementTemplate { Name = "Spinning Chorus", Order = 2 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Gustav Mahler",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Symphony No. 1 in D major 'Titan'", CatalogNumber = "Titan", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Langsam schleppend", Order = 1 }, new MovementTemplate { Name = "Kräftig bewegt", Order = 2 }, new MovementTemplate { Name = "Feierlich und gemessen", Order = 3 }, new MovementTemplate { Name = "Stürmisch bewegt", Order = 4 } } },
                    new WorkTemplate { Name = "Symphony No. 2 in C minor 'Resurrection'", CatalogNumber = "Resurrection", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro maestoso", Order = 1 }, new MovementTemplate { Name = "Andante moderato", Order = 2 }, new MovementTemplate { Name = "In ruhig fliessender Bewegung", Order = 3 }, new MovementTemplate { Name = "Urlicht", Order = 4 }, new MovementTemplate { Name = "Im Tempo des Scherzos", Order = 5 } } },
                    new WorkTemplate { Name = "Symphony No. 5 in C-sharp minor", CatalogNumber = "Mahler 5", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Trauermarsch", Order = 1 }, new MovementTemplate { Name = "Stürmisch bewegt", Order = 2 }, new MovementTemplate { Name = "Scherzo", Order = 3 }, new MovementTemplate { Name = "Adagietto", Order = 4 }, new MovementTemplate { Name = "Rondo-Finale", Order = 5 } } },
                    new WorkTemplate { Name = "Das Lied von der Erde", CatalogNumber = "Das Lied", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Das Trinklied vom Jammer der Erde", Order = 1 }, new MovementTemplate { Name = "Der Einsame im Herbst", Order = 2 }, new MovementTemplate { Name = "Von der Jugend", Order = 3 } } },
                    new WorkTemplate { Name = "Symphony No. 8 in E-flat major", CatalogNumber = "Symphony of a Thousand", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Part I: Veni, creator spiritus", Order = 1 }, new MovementTemplate { Name = "Part II: Final scene from Goethe's Faust", Order = 2 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Sergei Rachmaninoff",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Piano Concerto No. 2 in C minor", CatalogNumber = "Op. 18", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Moderato", Order = 1 }, new MovementTemplate { Name = "Adagio sostenuto", Order = 2 }, new MovementTemplate { Name = "Allegro scherzando", Order = 3 } } },
                    new WorkTemplate { Name = "Piano Concerto No. 3 in D minor", CatalogNumber = "Op. 30", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro ma non tanto", Order = 1 }, new MovementTemplate { Name = "Adagio", Order = 2 }, new MovementTemplate { Name = "Alla breve", Order = 3 } } },
                    new WorkTemplate { Name = "Rhapsody on a Theme of Paganini", CatalogNumber = "Op. 43", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Introduction", Order = 1 }, new MovementTemplate { Name = "Variation 18", Order = 2 }, new MovementTemplate { Name = "Finale", Order = 3 } } },
                    new WorkTemplate { Name = "Symphony No. 2 in E minor", CatalogNumber = "Op. 27", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Largo - Allegro", Order = 1 }, new MovementTemplate { Name = "Allegro molto", Order = 2 }, new MovementTemplate { Name = "Adagio", Order = 3 }, new MovementTemplate { Name = "Allegro vivace", Order = 4 } } },
                    new WorkTemplate { Name = "Prelude in C-sharp minor", CatalogNumber = "Op. 3 No. 2", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Lento", Order = 1 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Felix Mendelssohn",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Symphony No. 4 in A major 'Italian'", CatalogNumber = "Op. 90", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro vivace", Order = 1 }, new MovementTemplate { Name = "Andante con moto", Order = 2 }, new MovementTemplate { Name = "Con moto moderato", Order = 3 }, new MovementTemplate { Name = "Saltarello", Order = 4 } } },
                    new WorkTemplate { Name = "Violin Concerto in E minor", CatalogNumber = "Op. 64", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro molto appassionato", Order = 1 }, new MovementTemplate { Name = "Andante", Order = 2 }, new MovementTemplate { Name = "Allegro molto vivace", Order = 3 } } },
                    new WorkTemplate { Name = "A Midsummer Night's Dream", CatalogNumber = "Op. 61", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Overture", Order = 1 }, new MovementTemplate { Name = "Scherzo", Order = 2 }, new MovementTemplate { Name = "Nocturne", Order = 3 }, new MovementTemplate { Name = "Wedding March", Order = 4 } } },
                    new WorkTemplate { Name = "Octet in E-flat major", CatalogNumber = "Op. 20", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro con fuoco", Order = 1 }, new MovementTemplate { Name = "Andante", Order = 2 }, new MovementTemplate { Name = "Scherzo", Order = 3 }, new MovementTemplate { Name = "Presto", Order = 4 } } },
                    new WorkTemplate { Name = "Hebrides Overture 'Fingal's Cave'", CatalogNumber = "Op. 26", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Moderato", Order = 1 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Maurice Ravel",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Boléro", CatalogNumber = "M. 81", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Bolero", Order = 1 } } },
                    new WorkTemplate { Name = "Daphnis et Chloé", CatalogNumber = "M. 57", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Lever du jour", Order = 1 }, new MovementTemplate { Name = "Pantomime", Order = 2 }, new MovementTemplate { Name = "Danse générale", Order = 3 } } },
                    new WorkTemplate { Name = "Piano Concerto in G major", CatalogNumber = "M. 83", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegramente", Order = 1 }, new MovementTemplate { Name = "Adagio assai", Order = 2 }, new MovementTemplate { Name = "Presto", Order = 3 } } },
                    new WorkTemplate { Name = "Pavane pour une infante défunte", CatalogNumber = "M. 19", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Pavane", Order = 1 } } },
                    new WorkTemplate { Name = "Gaspard de la nuit", CatalogNumber = "M. 55", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Ondine", Order = 1 }, new MovementTemplate { Name = "Le Gibet", Order = 2 }, new MovementTemplate { Name = "Scarbo", Order = 3 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Antonio Vivaldi",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "The Four Seasons - Spring", CatalogNumber = "RV 269", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro", Order = 1 }, new MovementTemplate { Name = "Largo", Order = 2 }, new MovementTemplate { Name = "Allegro (Danza pastorale)", Order = 3 } } },
                    new WorkTemplate { Name = "The Four Seasons - Summer", CatalogNumber = "RV 315", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro non molto", Order = 1 }, new MovementTemplate { Name = "Adagio", Order = 2 }, new MovementTemplate { Name = "Presto", Order = 3 } } },
                    new WorkTemplate { Name = "The Four Seasons - Autumn", CatalogNumber = "RV 293", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro", Order = 1 }, new MovementTemplate { Name = "Adagio molto", Order = 2 }, new MovementTemplate { Name = "Allegro", Order = 3 } } },
                    new WorkTemplate { Name = "The Four Seasons - Winter", CatalogNumber = "RV 297", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro non molto", Order = 1 }, new MovementTemplate { Name = "Largo", Order = 2 }, new MovementTemplate { Name = "Allegro", Order = 3 } } },
                    new WorkTemplate { Name = "Gloria in D major", CatalogNumber = "RV 589", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Gloria in excelsis Deo", Order = 1 }, new MovementTemplate { Name = "Laudamus te", Order = 2 }, new MovementTemplate { Name = "Domine Deus", Order = 3 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "George Frideric Handel",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Messiah", CatalogNumber = "HWV 56", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Sinfony", Order = 1 }, new MovementTemplate { Name = "Comfort ye", Order = 2 }, new MovementTemplate { Name = "Ev'ry valley", Order = 3 }, new MovementTemplate { Name = "Hallelujah Chorus", Order = 4 } } },
                    new WorkTemplate { Name = "Water Music", CatalogNumber = "HWV 348-350", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro", Order = 1 }, new MovementTemplate { Name = "Air", Order = 2 }, new MovementTemplate { Name = "Bourrée", Order = 3 }, new MovementTemplate { Name = "Hornpipe", Order = 4 } } },
                    new WorkTemplate { Name = "Music for the Royal Fireworks", CatalogNumber = "HWV 351", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Ouverture", Order = 1 }, new MovementTemplate { Name = "La Paix", Order = 2 }, new MovementTemplate { Name = "La Réjouissance", Order = 3 }, new MovementTemplate { Name = "Menuet", Order = 4 } } },
                    new WorkTemplate { Name = "Rinaldo", CatalogNumber = "HWV 7", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Lascia ch'io pianga", Order = 1 } } },
                    new WorkTemplate { Name = "Xerxes", CatalogNumber = "HWV 40", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Ombra mai fu", Order = 1 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Franz Liszt",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Hungarian Rhapsody No. 2", CatalogNumber = "S. 244/2", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Lassan", Order = 1 }, new MovementTemplate { Name = "Friska", Order = 2 } } },
                    new WorkTemplate { Name = "Piano Sonata in B minor", CatalogNumber = "S. 178", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Lento - Allegro", Order = 1 }, new MovementTemplate { Name = "Andante sostenuto", Order = 2 }, new MovementTemplate { Name = "Allegro energico", Order = 3 } } },
                    new WorkTemplate { Name = "Les Préludes", CatalogNumber = "S. 97", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Les Preludes", Order = 1 } } },
                    new WorkTemplate { Name = "Liebesträume No. 3", CatalogNumber = "S. 541/3", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Poco allegro con affetto", Order = 1 } } },
                    new WorkTemplate { Name = "Transcendental Études", CatalogNumber = "S. 139", Movements = new MovementTemplate[] { new MovementTemplate { Name = "No. 4 Mazeppa", Order = 1 }, new MovementTemplate { Name = "No. 5 Feux follets", Order = 2 }, new MovementTemplate { Name = "No. 11 Harmonies du soir", Order = 3 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Robert Schumann",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Dichterliebe", CatalogNumber = "Op. 48", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Im wunderschönen Monat Mai", Order = 1 }, new MovementTemplate { Name = "Ich grolle nicht", Order = 2 } } },
                    new WorkTemplate { Name = "Carnaval", CatalogNumber = "Op. 9", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Préambule", Order = 1 }, new MovementTemplate { Name = "Pierrot", Order = 2 }, new MovementTemplate { Name = "Arlequin", Order = 3 }, new MovementTemplate { Name = "Chopin", Order = 4 } } },
                    new WorkTemplate { Name = "Piano Concerto in A minor", CatalogNumber = "Op. 54", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro affettuoso", Order = 1 }, new MovementTemplate { Name = "Intermezzo", Order = 2 }, new MovementTemplate { Name = "Allegro vivace", Order = 3 } } },
                    new WorkTemplate { Name = "Symphony No. 3 in E-flat major 'Rhenish'", CatalogNumber = "Op. 97", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Lebhaft", Order = 1 }, new MovementTemplate { Name = "Scherzo", Order = 2 }, new MovementTemplate { Name = "Nicht schnell", Order = 3 }, new MovementTemplate { Name = "Feierlich", Order = 4 }, new MovementTemplate { Name = "Lebhaft", Order = 5 } } },
                    new WorkTemplate { Name = "Kinderszenen", CatalogNumber = "Op. 15", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Von fremden Ländern und Menschen", Order = 1 }, new MovementTemplate { Name = "Träumerei", Order = 2 } } }
                }
            },
            new ComposerTemplate
            {
                Name = "Antonín Dvořák",
                Works = new WorkTemplate[]
                {
                    new WorkTemplate { Name = "Symphony No. 9 in E minor 'From the New World'", CatalogNumber = "Op. 95", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Adagio - Allegro", Order = 1 }, new MovementTemplate { Name = "Largo", Order = 2 }, new MovementTemplate { Name = "Scherzo", Order = 3 }, new MovementTemplate { Name = "Allegro con fuoco", Order = 4 } } },
                    new WorkTemplate { Name = "Cello Concerto in B minor", CatalogNumber = "Op. 104", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro", Order = 1 }, new MovementTemplate { Name = "Adagio ma non troppo", Order = 2 }, new MovementTemplate { Name = "Finale", Order = 3 } } },
                    new WorkTemplate { Name = "Slavonic Dances", CatalogNumber = "Op. 46", Movements = new MovementTemplate[] { new MovementTemplate { Name = "No. 1 in C major", Order = 1 }, new MovementTemplate { Name = "No. 8 in G minor", Order = 2 } } },
                    new WorkTemplate { Name = "String Quartet No. 12 in F major 'American'", CatalogNumber = "Op. 96", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Allegro ma non troppo", Order = 1 }, new MovementTemplate { Name = "Lento", Order = 2 }, new MovementTemplate { Name = "Molto vivace", Order = 3 }, new MovementTemplate { Name = "Finale", Order = 4 } } },
                    new WorkTemplate { Name = "Rusalka", CatalogNumber = "Op. 114", Movements = new MovementTemplate[] { new MovementTemplate { Name = "Song to the Moon", Order = 1 } } }
                }
            }
        };
    }
}
