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
        public MovementTemplate[] Movements { get; set; } = [];
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
        public WorkTemplate[] Works { get; set; } = [];
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
        List<Composer> addedComposers = [];
        List<Work> addedWorks = [];
        List<Movement> addedMovements = [];

        int workOffset = 0;

        foreach (ComposerTemplate composerTemplate in templates)
        {
            Composer composer = new()
            {
                Name = composerTemplate.Name
            };
            await uow.Composers.AddAsync(composer);
            await uow.SaveChangesAsync();
            addedComposers.Add(composer);

            foreach (WorkTemplate workTemplate in composerTemplate.Works)
            {
                Work work = new()
                {
                    ComposerId = composer.Id,
                    CatalogNumber = workTemplate.CatalogNumber,
                    Name = workTemplate.Name
                };
                await uow.Works.AddAsync(work);
                await uow.SaveChangesAsync();
                addedWorks.Add(work);

                List<Movement> workMovements = [];
                foreach (MovementTemplate movementTemplate in workTemplate.Movements)
                {
                    Movement movement = new()
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
                    AudioFile audioFile = new()
                    {
                        Path = $"/music/{composer.Name.Replace(" ", "_")}/{work.Name.Replace(" ", "_")}/track_{_audioFileCount++:D3}.flac",
                        Hash = (ulong)_audioFileCount * 123456789UL,
                        Status = IngestionStatus.Complete
                    };
                    await uow.AudioFiles.AddAsync(audioFile);
                    await uow.SaveChangesAsync();

                    PerformedMovement performedMovement = new()
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
            Recording altRecording = new()
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
                AudioFile audioFile = new()
                {
                    Path = $"/music/Beethoven/Symphony_5_Kleiber/track_{_audioFileCount++:D3}.flac",
                    Hash = (ulong)_audioFileCount * 987654321UL,
                    Status = IngestionStatus.Complete
                };
                await uow.AudioFiles.AddAsync(audioFile);
                await uow.SaveChangesAsync();

                PerformedMovement performedMovement = new()
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
            Recording altRecording = new()
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
                AudioFile audioFile = new()
                {
                    Path = $"/music/Bach/Goldberg_Gould_1955/track_{_audioFileCount++:D3}.flac",
                    Hash = (ulong)_audioFileCount * 987654321UL,
                    Status = IngestionStatus.Complete
                };
                await uow.AudioFiles.AddAsync(audioFile);
                await uow.SaveChangesAsync();

                PerformedMovement performedMovement = new()
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
            Recording seasonsRecording = new()
            {
                WorkId = spring.Id,
                Conductor = "Neville Marriner",
                Ensemble = "Academy of St Martin in the Fields",
                Soloist = "Alan Loveday",
                Year = 1969
            };
            await uow.Recordings.AddAsync(seasonsRecording);
            await uow.SaveChangesAsync();

            List<Work> seasonsWorks = [spring, summer, autumn, winter];
            int orderCounter = 1;
            foreach (Work work in seasonsWorks)
            {
                List<Movement> workMovements = movements.Where(m => m.WorkId == work.Id).OrderBy(m => m.Order).ToList();
                foreach (Movement movement in workMovements)
                {
                    AudioFile audioFile = new()
                    {
                        Path = $"/music/Vivaldi/Four_Seasons_Marriner/track_{_audioFileCount++:D3}.flac",
                        Hash = (ulong)_audioFileCount * 1122334455UL,
                        Status = IngestionStatus.Complete
                    };
                    await uow.AudioFiles.AddAsync(audioFile);
                    await uow.SaveChangesAsync();

                    PerformedMovement performedMovement = new()
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
            Recording violinConcertosRecording = new()
            {
                WorkId = beethovenViolin.Id,
                Conductor = "Jascha Heifetz",
                Ensemble = "Boston Symphony Orchestra",
                Year = 1955
            };
            await uow.Recordings.AddAsync(violinConcertosRecording);
            await uow.SaveChangesAsync();

            List<Work> concertosWorks = [beethovenViolin, brahmsViolin];
            int orderCounter = 1;
            foreach (Work work in concertosWorks)
            {
                List<Movement> workMovements = movements.Where(m => m.WorkId == work.Id).OrderBy(m => m.Order).ToList();
                foreach (Movement movement in workMovements)
                {
                    AudioFile audioFile = new()
                    {
                        Path = $"/music/Concertos/Heifetz_Violin_Concertos/track_{_audioFileCount++:D3}.flac",
                        Hash = (ulong)_audioFileCount * 9988776655UL,
                        Status = IngestionStatus.Complete
                    };
                    await uow.AudioFiles.AddAsync(audioFile);
                    await uow.SaveChangesAsync();

                    PerformedMovement performedMovement = new()
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
            string[] ensembles = ["Alban Berg Quartett", "Emerson String Quartet", "Takács Quartet", "Juilliard String Quartet", "Borodin Quartet"
            ];
            ensemble = ensembles[seedOffset % ensembles.Length];
            year = 1980 + (seedOffset % 30);
        }
        else if (workNameLower.Contains("requiem") || workNameLower.Contains("passion") || workNameLower.Contains("mass") || 
                 workNameLower.Contains("traviata") || workNameLower.Contains("aida") || workNameLower.Contains("rigoletto") || 
                 workNameLower.Contains("otello") || workNameLower.Contains("opera") || workNameLower.Contains("messiah") || 
                 workNameLower.Contains("gloria") || workNameLower.Contains("rusalka"))
        {
            string[] conductors = ["John Eliot Gardiner", "Karl Richter", "Georg Solti", "Herbert von Karajan", "Claudio Abbado"
            ];
            string[] ensembles = ["English Baroque Soloists & Monteverdi Choir", "Munich Bach Orchestra", "Vienna State Opera Orchestra", "Berlin Philharmonic Orchestra", "London Symphony Chorus & Orchestra"
            ];
            conductor = conductors[seedOffset % conductors.Length];
            ensemble = ensembles[seedOffset % ensembles.Length];
            year = 1970 + (seedOffset % 35);
        }
        else if (workNameLower.Contains("concerto") || workNameLower.Contains("rhapsody on a theme"))
        {
            string[] conductors = ["Leonard Bernstein", "Bernard Haitink", "Claudio Abbado", "Georg Solti", "Zubin Mehta"
            ];
            string[] ensembles = ["Chicago Symphony Orchestra", "Vienna Philharmonic Orchestra", "London Symphony Orchestra", "Royal Concertgebouw Orchestra", "Boston Symphony Orchestra"
            ];
            
            conductor = conductors[seedOffset % conductors.Length];
            ensemble = ensembles[seedOffset % ensembles.Length];

            if (workNameLower.Contains("violin"))
            {
                string[] soloists = ["Itzhak Perlman", "Anne-Sophie Mutter", "Jascha Heifetz", "David Oistrakh", "Joshua Bell"
                ];
                soloist = soloists[seedOffset % soloists.Length];
            }
            else if (workNameLower.Contains("cello"))
            {
                string[] soloists = ["Mstislav Rostropovich", "Jacqueline du Pré", "Yo-Yo Ma", "Jian Wang"];
                soloist = soloists[seedOffset % soloists.Length];
            }
            else
            {
                string[] soloists = ["Martha Argerich", "Vladimir Ashkenazy", "Krystian Zimerman", "Alfred Brendel", "Evgeny Kissin"
                ];
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
                string[] soloists = ["Mstislav Rostropovich", "Yo-Yo Ma", "Pierre Fournier", "Pablo Casals"];
                soloist = soloists[seedOffset % soloists.Length];
            }
            else
            {
                string[] soloists = ["Vladimir Horowitz", "Arthur Rubinstein", "Glenn Gould", "Sviatoslav Richter", "Martha Argerich", "Maurizio Pollini", "Claudio Arrau"
                ];
                soloist = soloists[seedOffset % soloists.Length];
            }
            year = 1960 + (seedOffset % 45);
        }
        else
        {
            string[] conductors = ["Herbert von Karajan", "Leonard Bernstein", "Carlos Kleiber", "Pierre Boulez", "Simon Rattle", "Valery Gergiev", "Neville Marriner"
            ];
            string[] ensembles = ["Berlin Philharmonic Orchestra", "Vienna Philharmonic Orchestra", "Chicago Symphony Orchestra", "London Symphony Orchestra", "Cleveland Orchestra", "New York Philharmonic"
            ];
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
        return
        [
            new()
            {
                Name = "Johann Sebastian Bach",
                Works =
                [
                    new() { Name = "St Matthew Passion", CatalogNumber = "BWV 244", Movements = [new() { Name = "Part I", Order = 1 }, new() { Name = "Part II", Order = 2 }
                        ]
                    },
                    new() { Name = "Goldberg Variations", CatalogNumber = "BWV 988", Movements = [new() { Name = "Aria", Order = 1 }, new() { Name = "Variations", Order = 2 }, new() { Name = "Aria da Capo", Order = 3 }
                        ]
                    },
                    new() { Name = "Cello Suite No. 1 in G major", CatalogNumber = "BWV 1007", Movements = [new() { Name = "Prelude", Order = 1 }, new() { Name = "Allemande", Order = 2 }, new() { Name = "Courante", Order = 3 }, new() { Name = "Sarabande", Order = 4 }, new() { Name = "Minuets", Order = 5 }, new() { Name = "Gigue", Order = 6 }
                        ]
                    },
                    new() { Name = "Brandenburg Concerto No. 3 in G major", CatalogNumber = "BWV 1048", Movements = [new() { Name = "Allegro", Order = 1 }, new() { Name = "Adagio", Order = 2 }, new() { Name = "Allegro", Order = 3 }
                        ]
                    },
                    new() { Name = "Mass in B minor", CatalogNumber = "BWV 232", Movements = [new() { Name = "Kyrie", Order = 1 }, new() { Name = "Gloria", Order = 2 }, new() { Name = "Credo", Order = 3 }, new() { Name = "Sanctus", Order = 4 }, new() { Name = "Agnus Dei", Order = 5 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Wolfgang Amadeus Mozart",
                Works =
                [
                    new() { Name = "Symphony No. 41 in C major 'Jupiter'", CatalogNumber = "K. 551", Movements = [new() { Name = "Allegro vivace", Order = 1 }, new() { Name = "Andante cantabile", Order = 2 }, new() { Name = "Menuetto", Order = 3 }, new() { Name = "Molto allegro", Order = 4 }
                        ]
                    },
                    new() { Name = "Piano Concerto No. 21 in C major", CatalogNumber = "K. 467", Movements = [new() { Name = "Allegro maestoso", Order = 1 }, new() { Name = "Andante", Order = 2 }, new() { Name = "Allegro vivace assai", Order = 3 }
                        ]
                    },
                    new() { Name = "Requiem in D minor", CatalogNumber = "K. 626", Movements = [new() { Name = "Introitus", Order = 1 }, new() { Name = "Kyrie", Order = 2 }, new() { Name = "Sequentia", Order = 3 }, new() { Name = "Offertorium", Order = 4 }, new() { Name = "Sanctus", Order = 5 }, new() { Name = "Benedictus", Order = 6 }, new() { Name = "Agnus Dei", Order = 7 }, new() { Name = "Communio", Order = 8 }
                        ]
                    },
                    new() { Name = "The Marriage of Figaro", CatalogNumber = "K. 492", Movements = [new() { Name = "Overture", Order = 1 }, new() { Name = "Act I", Order = 2 }, new() { Name = "Act II", Order = 3 }, new() { Name = "Act III", Order = 4 }, new() { Name = "Act IV", Order = 5 }
                        ]
                    },
                    new() { Name = "Clarinet Concerto in A major", CatalogNumber = "K. 622", Movements = [new() { Name = "Allegro", Order = 1 }, new() { Name = "Adagio", Order = 2 }, new() { Name = "Rondo", Order = 3 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Ludwig van Beethoven",
                Works =
                [
                    new() { Name = "Symphony No. 5 in C minor", CatalogNumber = "Op. 67", Movements = [new() { Name = "Allegro con brio", Order = 1 }, new() { Name = "Andante con moto", Order = 2 }, new() { Name = "Scherzo", Order = 3 }, new() { Name = "Allegro", Order = 4 }
                        ]
                    },
                    new() { Name = "Symphony No. 9 in D minor", CatalogNumber = "Op. 125", Movements = [new() { Name = "Allegro ma non troppo", Order = 1 }, new() { Name = "Molto vivace", Order = 2 }, new() { Name = "Adagio molto", Order = 3 }, new() { Name = "Finale", Order = 4 }
                        ]
                    },
                    new() { Name = "Piano Sonata No. 14 'Moonlight'", CatalogNumber = "Op. 27 No. 2", Movements = [new() { Name = "Adagio sostenuto", Order = 1 }, new() { Name = "Allegretto", Order = 2 }, new() { Name = "Presto agitato", Order = 3 }
                        ]
                    },
                    new() { Name = "Violin Concerto in D major", CatalogNumber = "Op. 61", Movements = [new() { Name = "Allegro ma non troppo", Order = 1 }, new() { Name = "Larghetto", Order = 2 }, new() { Name = "Rondo", Order = 3 }
                        ]
                    },
                    new() { Name = "Piano Concerto No. 5 'Emperor'", CatalogNumber = "Op. 73", Movements = [new() { Name = "Allegro", Order = 1 }, new() { Name = "Adagio un poco mosso", Order = 2 }, new() { Name = "Rondo", Order = 3 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Franz Schubert",
                Works =
                [
                    new() { Name = "Symphony No. 8 in B minor 'Unfinished'", CatalogNumber = "D. 759", Movements = [new() { Name = "Allegro moderato", Order = 1 }, new() { Name = "Andante con moto", Order = 2 }
                        ]
                    },
                    new() { Name = "Winterreise", CatalogNumber = "D. 911", Movements = [new() { Name = "Gute Nacht", Order = 1 }, new() { Name = "Der Lindenbaum", Order = 2 }, new() { Name = "Die Post", Order = 3 }, new() { Name = "Der Leiermann", Order = 4 }
                        ]
                    },
                    new() { Name = "String Quintet in C major", CatalogNumber = "D. 956", Movements = [new() { Name = "Allegro ma non troppo", Order = 1 }, new() { Name = "Adagio", Order = 2 }, new() { Name = "Scherzo", Order = 3 }, new() { Name = "Allegretto", Order = 4 }
                        ]
                    },
                    new() { Name = "Piano Quintet in A major 'Trout'", CatalogNumber = "D. 667", Movements = [new() { Name = "Allegro vivace", Order = 1 }, new() { Name = "Andante", Order = 2 }, new() { Name = "Scherzo", Order = 3 }, new() { Name = "Tema con variazioni", Order = 4 }, new() { Name = "Finale", Order = 5 }
                        ]
                    },
                    new() { Name = "Ave Maria", CatalogNumber = "D. 839", Movements = [new() { Name = "Ave Maria", Order = 1 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Frédéric Chopin",
                Works =
                [
                    new() { Name = "Nocturnes, Op. 9", CatalogNumber = "Op. 9", Movements = [new() { Name = "No. 1 in B-flat minor", Order = 1 }, new() { Name = "No. 2 in E-flat major", Order = 2 }, new() { Name = "No. 3 in B major", Order = 3 }
                        ]
                    },
                    new() { Name = "Ballade No. 1 in G minor", CatalogNumber = "Op. 23", Movements = [new() { Name = "Ballade", Order = 1 }
                        ]
                    },
                    new() { Name = "Preludes, Op. 28", CatalogNumber = "Op. 28", Movements = [new() { Name = "No. 4 in E minor", Order = 1 }, new() { Name = "No. 15 'Raindrop'", Order = 2 }, new() { Name = "No. 20 in C minor", Order = 3 }
                        ]
                    },
                    new() { Name = "Piano Sonata No. 2 in B-flat minor", CatalogNumber = "Op. 35", Movements = [new() { Name = "Grave", Order = 1 }, new() { Name = "Scherzo", Order = 2 }, new() { Name = "Marche funèbre", Order = 3 }, new() { Name = "Presto", Order = 4 }
                        ]
                    },
                    new() { Name = "Fantaisie-Impromptu", CatalogNumber = "Op. 66", Movements = [new() { Name = "Moderato cantabile", Order = 1 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Johannes Brahms",
                Works =
                [
                    new() { Name = "Symphony No. 1 in C minor", CatalogNumber = "Op. 68", Movements = [new() { Name = "Un poco sostenuto", Order = 1 }, new() { Name = "Andante sostenuto", Order = 2 }, new() { Name = "Un poco allegretto", Order = 3 }, new() { Name = "Adagio", Order = 4 }
                        ]
                    },
                    new() { Name = "Violin Concerto in D major", CatalogNumber = "Op. 77", Movements = [new() { Name = "Allegro non troppo", Order = 1 }, new() { Name = "Adagio", Order = 2 }, new() { Name = "Allegro giocoso", Order = 3 }
                        ]
                    },
                    new() { Name = "A German Requiem", CatalogNumber = "Op. 45", Movements = [new() { Name = "Selig sind", Order = 1 }, new() { Name = "Denn alles Fleisch", Order = 2 }, new() { Name = "Herr, lehre doch mich", Order = 3 }
                        ]
                    },
                    new() { Name = "Piano Concerto No. 2 in B-flat major", CatalogNumber = "Op. 83", Movements = [new() { Name = "Allegro non troppo", Order = 1 }, new() { Name = "Allegro appassionato", Order = 2 }, new() { Name = "Andante", Order = 3 }, new() { Name = "Allegretto grazioso", Order = 4 }
                        ]
                    },
                    new() { Name = "Hungarian Dances", CatalogNumber = "WoO 1", Movements = [new() { Name = "No. 1 in G minor", Order = 1 }, new() { Name = "No. 5 in F-sharp minor", Order = 2 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Pyotr Ilyich Tchaikovsky",
                Works =
                [
                    new() { Name = "Symphony No. 6 in B minor 'Pathétique'", CatalogNumber = "Op. 74", Movements = [new() { Name = "Adagio", Order = 1 }, new() { Name = "Allegro con grazia", Order = 2 }, new() { Name = "Allegro molto vivace", Order = 3 }, new() { Name = "Finale", Order = 4 }
                        ]
                    },
                    new() { Name = "The Nutcracker", CatalogNumber = "Op. 71", Movements = [new() { Name = "Overture", Order = 1 }, new() { Name = "March", Order = 2 }, new() { Name = "Dance of the Sugar Plum Fairy", Order = 3 }, new() { Name = "Russian Dance", Order = 4 }, new() { Name = "Waltz of the Flowers", Order = 5 }
                        ]
                    },
                    new() { Name = "Swan Lake", CatalogNumber = "Op. 20", Movements = [new() { Name = "Scene", Order = 1 }, new() { Name = "Waltz", Order = 2 }, new() { Name = "Dance of the Swans", Order = 3 }, new() { Name = "Hungarian Dance", Order = 4 }
                        ]
                    },
                    new() { Name = "Violin Concerto in D major", CatalogNumber = "Op. 35", Movements = [new() { Name = "Allegro moderato", Order = 1 }, new() { Name = "Canzonetta", Order = 2 }, new() { Name = "Finale", Order = 3 }
                        ]
                    },
                    new() { Name = "Piano Concerto No. 1 in B-flat minor", CatalogNumber = "Op. 23", Movements = [new() { Name = "Allegro non troppo", Order = 1 }, new() { Name = "Andantino simplice", Order = 2 }, new() { Name = "Allegro con fuoco", Order = 3 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Claude Debussy",
                Works =
                [
                    new() { Name = "La Mer", CatalogNumber = "L. 111", Movements = [new() { Name = "De l'aube à midi sur la mer", Order = 1 }, new() { Name = "Jeux de vagues", Order = 2 }, new() { Name = "Dialogue du vent et de la mer", Order = 3 }
                        ]
                    },
                    new() { Name = "Prélude à l'après-midi d'un faune", CatalogNumber = "L. 87", Movements = [new() { Name = "Prélude", Order = 1 }
                        ]
                    },
                    new() { Name = "Suite bergamasque", CatalogNumber = "L. 75", Movements = [new() { Name = "Prélude", Order = 1 }, new() { Name = "Menuet", Order = 2 }, new() { Name = "Clair de lune", Order = 3 }, new() { Name = "Passepied", Order = 4 }
                        ]
                    },
                    new() { Name = "Nocturnes", CatalogNumber = "L. 91", Movements = [new() { Name = "Nuages", Order = 1 }, new() { Name = "Fêtes", Order = 2 }, new() { Name = "Sirènes", Order = 3 }
                        ]
                    },
                    new() { Name = "String Quartet in G minor", CatalogNumber = "Op. 10", Movements = [new() { Name = "Animé et très décidé", Order = 1 }, new() { Name = "Assez vif", Order = 2 }, new() { Name = "Andantino doucement expressif", Order = 3 }, new() { Name = "Très modéré", Order = 4 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Igor Stravinsky",
                Works =
                [
                    new() { Name = "The Rite of Spring", CatalogNumber = "K015", Movements = [new() { Name = "Part I: Adoration of the Earth", Order = 1 }, new() { Name = "Part II: The Sacrifice", Order = 2 }
                        ]
                    },
                    new() { Name = "The Firebird", CatalogNumber = "K010", Movements = [new() { Name = "Introduction", Order = 1 }, new() { Name = "Dance of the Firebird", Order = 2 }, new() { Name = "Infernal Dance", Order = 3 }, new() { Name = "Lullaby", Order = 4 }, new() { Name = "Finale", Order = 5 }
                        ]
                    },
                    new() { Name = "Petrushka", CatalogNumber = "K012", Movements = [new() { Name = "The Shrovetide Fair", Order = 1 }, new() { Name = "Petrushka's Room", Order = 2 }, new() { Name = "The Moor's Room", Order = 3 }, new() { Name = "The Shrovetide Fair - Evening", Order = 4 }
                        ]
                    },
                    new() { Name = "Symphony of Psalms", CatalogNumber = "K053", Movements = [new() { Name = "Part I", Order = 1 }, new() { Name = "Part II", Order = 2 }, new() { Name = "Part III", Order = 3 }
                        ]
                    },
                    new() { Name = "L'Histoire du soldat", CatalogNumber = "K029", Movements = [new() { Name = "Soldier's March", Order = 1 }, new() { Name = "Music to Scene 1", Order = 2 }, new() { Name = "Music to Scene 2", Order = 3 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Giuseppe Verdi",
                Works =
                [
                    new() { Name = "Messa da Requiem", CatalogNumber = "Verdi Requiem", Movements = [new() { Name = "Requiem aeternam", Order = 1 }, new() { Name = "Dies irae", Order = 2 }, new() { Name = "Offertorio", Order = 3 }, new() { Name = "Sanctus", Order = 4 }, new() { Name = "Agnus Dei", Order = 5 }
                        ]
                    },
                    new() { Name = "La Traviata", CatalogNumber = "Opera", Movements = [new() { Name = "Overture", Order = 1 }, new() { Name = "Libiamo ne' lieti calici", Order = 2 }, new() { Name = "Sempre libera", Order = 3 }
                        ]
                    },
                    new() { Name = "Aida", CatalogNumber = "Opera", Movements = [new() { Name = "Prelude", Order = 1 }, new() { Name = "Celeste Aida", Order = 2 }, new() { Name = "Grand March", Order = 3 }
                        ]
                    },
                    new() { Name = "Rigoletto", CatalogNumber = "Opera", Movements = [new() { Name = "Questa o quella", Order = 1 }, new() { Name = "Caro nome", Order = 2 }, new() { Name = "La donna è mobile", Order = 3 }
                        ]
                    },
                    new() { Name = "Otello", CatalogNumber = "Opera", Movements = [new() { Name = "Esultate", Order = 1 }, new() { Name = "Gia nella notte", Order = 2 }, new() { Name = "Ave Maria", Order = 3 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Richard Wagner",
                Works =
                [
                    new() { Name = "Tristan und Isolde", CatalogNumber = "WWV 90", Movements = [new() { Name = "Prelude", Order = 1 }, new() { Name = "Mild und leise (Liebestod)", Order = 2 }
                        ]
                    },
                    new() { Name = "Die Walküre", CatalogNumber = "WWV 86B", Movements = [new() { Name = "Ride of the Valkyries", Order = 1 }, new() { Name = "Wotan's Farewell", Order = 2 }
                        ]
                    },
                    new() { Name = "Tannhäuser", CatalogNumber = "WWV 70", Movements = [new() { Name = "Overture", Order = 1 }, new() { Name = "Pilgrim's Chorus", Order = 2 }
                        ]
                    },
                    new() { Name = "Lohengrin", CatalogNumber = "WWV 75", Movements = [new() { Name = "Prelude to Act I", Order = 1 }, new() { Name = "Bridal Chorus", Order = 2 }, new() { Name = "Prelude to Act III", Order = 3 }
                        ]
                    },
                    new() { Name = "Der Fliegende Holländer", CatalogNumber = "WWV 63", Movements = [new() { Name = "Overture", Order = 1 }, new() { Name = "Spinning Chorus", Order = 2 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Gustav Mahler",
                Works =
                [
                    new() { Name = "Symphony No. 1 in D major 'Titan'", CatalogNumber = "Titan", Movements = [new() { Name = "Langsam schleppend", Order = 1 }, new() { Name = "Kräftig bewegt", Order = 2 }, new() { Name = "Feierlich und gemessen", Order = 3 }, new() { Name = "Stürmisch bewegt", Order = 4 }
                        ]
                    },
                    new() { Name = "Symphony No. 2 in C minor 'Resurrection'", CatalogNumber = "Resurrection", Movements =
                        [new() { Name = "Allegro maestoso", Order = 1 }, new() { Name = "Andante moderato", Order = 2 }, new() { Name = "In ruhig fliessender Bewegung", Order = 3 }, new() { Name = "Urlicht", Order = 4 }, new() { Name = "Im Tempo des Scherzos", Order = 5 }
                        ]
                    },
                    new() { Name = "Symphony No. 5 in C-sharp minor", CatalogNumber = "Mahler 5", Movements = [new() { Name = "Trauermarsch", Order = 1 }, new() { Name = "Stürmisch bewegt", Order = 2 }, new() { Name = "Scherzo", Order = 3 }, new() { Name = "Adagietto", Order = 4 }, new() { Name = "Rondo-Finale", Order = 5 }
                        ]
                    },
                    new() { Name = "Das Lied von der Erde", CatalogNumber = "Das Lied", Movements = [new() { Name = "Das Trinklied vom Jammer der Erde", Order = 1 }, new() { Name = "Der Einsame im Herbst", Order = 2 }, new() { Name = "Von der Jugend", Order = 3 }
                        ]
                    },
                    new() { Name = "Symphony No. 8 in E-flat major", CatalogNumber = "Symphony of a Thousand", Movements =
                        [new() { Name = "Part I: Veni, creator spiritus", Order = 1 }, new() { Name = "Part II: Final scene from Goethe's Faust", Order = 2 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Sergei Rachmaninoff",
                Works =
                [
                    new() { Name = "Piano Concerto No. 2 in C minor", CatalogNumber = "Op. 18", Movements = [new() { Name = "Moderato", Order = 1 }, new() { Name = "Adagio sostenuto", Order = 2 }, new() { Name = "Allegro scherzando", Order = 3 }
                        ]
                    },
                    new() { Name = "Piano Concerto No. 3 in D minor", CatalogNumber = "Op. 30", Movements = [new() { Name = "Allegro ma non tanto", Order = 1 }, new() { Name = "Adagio", Order = 2 }, new() { Name = "Alla breve", Order = 3 }
                        ]
                    },
                    new() { Name = "Rhapsody on a Theme of Paganini", CatalogNumber = "Op. 43", Movements = [new() { Name = "Introduction", Order = 1 }, new() { Name = "Variation 18", Order = 2 }, new() { Name = "Finale", Order = 3 }
                        ]
                    },
                    new() { Name = "Symphony No. 2 in E minor", CatalogNumber = "Op. 27", Movements = [new() { Name = "Largo - Allegro", Order = 1 }, new() { Name = "Allegro molto", Order = 2 }, new() { Name = "Adagio", Order = 3 }, new() { Name = "Allegro vivace", Order = 4 }
                        ]
                    },
                    new() { Name = "Prelude in C-sharp minor", CatalogNumber = "Op. 3 No. 2", Movements = [new() { Name = "Lento", Order = 1 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Felix Mendelssohn",
                Works =
                [
                    new() { Name = "Symphony No. 4 in A major 'Italian'", CatalogNumber = "Op. 90", Movements = [new() { Name = "Allegro vivace", Order = 1 }, new() { Name = "Andante con moto", Order = 2 }, new() { Name = "Con moto moderato", Order = 3 }, new() { Name = "Saltarello", Order = 4 }
                        ]
                    },
                    new() { Name = "Violin Concerto in E minor", CatalogNumber = "Op. 64", Movements = [new() { Name = "Allegro molto appassionato", Order = 1 }, new() { Name = "Andante", Order = 2 }, new() { Name = "Allegro molto vivace", Order = 3 }
                        ]
                    },
                    new() { Name = "A Midsummer Night's Dream", CatalogNumber = "Op. 61", Movements = [new() { Name = "Overture", Order = 1 }, new() { Name = "Scherzo", Order = 2 }, new() { Name = "Nocturne", Order = 3 }, new() { Name = "Wedding March", Order = 4 }
                        ]
                    },
                    new() { Name = "Octet in E-flat major", CatalogNumber = "Op. 20", Movements = [new() { Name = "Allegro con fuoco", Order = 1 }, new() { Name = "Andante", Order = 2 }, new() { Name = "Scherzo", Order = 3 }, new() { Name = "Presto", Order = 4 }
                        ]
                    },
                    new() { Name = "Hebrides Overture 'Fingal's Cave'", CatalogNumber = "Op. 26", Movements = [new() { Name = "Moderato", Order = 1 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Maurice Ravel",
                Works =
                [
                    new() { Name = "Boléro", CatalogNumber = "M. 81", Movements = [new() { Name = "Bolero", Order = 1 }]
                    },
                    new() { Name = "Daphnis et Chloé", CatalogNumber = "M. 57", Movements = [new() { Name = "Lever du jour", Order = 1 }, new() { Name = "Pantomime", Order = 2 }, new() { Name = "Danse générale", Order = 3 }
                        ]
                    },
                    new() { Name = "Piano Concerto in G major", CatalogNumber = "M. 83", Movements = [new() { Name = "Allegramente", Order = 1 }, new() { Name = "Adagio assai", Order = 2 }, new() { Name = "Presto", Order = 3 }
                        ]
                    },
                    new() { Name = "Pavane pour une infante défunte", CatalogNumber = "M. 19", Movements = [new() { Name = "Pavane", Order = 1 }
                        ]
                    },
                    new() { Name = "Gaspard de la nuit", CatalogNumber = "M. 55", Movements = [new() { Name = "Ondine", Order = 1 }, new() { Name = "Le Gibet", Order = 2 }, new() { Name = "Scarbo", Order = 3 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Antonio Vivaldi",
                Works =
                [
                    new() { Name = "The Four Seasons - Spring", CatalogNumber = "RV 269", Movements = [new() { Name = "Allegro", Order = 1 }, new() { Name = "Largo", Order = 2 }, new() { Name = "Allegro (Danza pastorale)", Order = 3 }
                        ]
                    },
                    new() { Name = "The Four Seasons - Summer", CatalogNumber = "RV 315", Movements = [new() { Name = "Allegro non molto", Order = 1 }, new() { Name = "Adagio", Order = 2 }, new() { Name = "Presto", Order = 3 }
                        ]
                    },
                    new() { Name = "The Four Seasons - Autumn", CatalogNumber = "RV 293", Movements = [new() { Name = "Allegro", Order = 1 }, new() { Name = "Adagio molto", Order = 2 }, new() { Name = "Allegro", Order = 3 }
                        ]
                    },
                    new() { Name = "The Four Seasons - Winter", CatalogNumber = "RV 297", Movements = [new() { Name = "Allegro non molto", Order = 1 }, new() { Name = "Largo", Order = 2 }, new() { Name = "Allegro", Order = 3 }
                        ]
                    },
                    new() { Name = "Gloria in D major", CatalogNumber = "RV 589", Movements = [new() { Name = "Gloria in excelsis Deo", Order = 1 }, new() { Name = "Laudamus te", Order = 2 }, new() { Name = "Domine Deus", Order = 3 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "George Frideric Handel",
                Works =
                [
                    new() { Name = "Messiah", CatalogNumber = "HWV 56", Movements = [new() { Name = "Sinfony", Order = 1 }, new() { Name = "Comfort ye", Order = 2 }, new() { Name = "Ev'ry valley", Order = 3 }, new() { Name = "Hallelujah Chorus", Order = 4 }
                        ]
                    },
                    new() { Name = "Water Music", CatalogNumber = "HWV 348-350", Movements = [new() { Name = "Allegro", Order = 1 }, new() { Name = "Air", Order = 2 }, new() { Name = "Bourrée", Order = 3 }, new() { Name = "Hornpipe", Order = 4 }
                        ]
                    },
                    new() { Name = "Music for the Royal Fireworks", CatalogNumber = "HWV 351", Movements = [new() { Name = "Ouverture", Order = 1 }, new() { Name = "La Paix", Order = 2 }, new() { Name = "La Réjouissance", Order = 3 }, new() { Name = "Menuet", Order = 4 }
                        ]
                    },
                    new() { Name = "Rinaldo", CatalogNumber = "HWV 7", Movements = [new() { Name = "Lascia ch'io pianga", Order = 1 }
                        ]
                    },
                    new() { Name = "Xerxes", CatalogNumber = "HWV 40", Movements = [new() { Name = "Ombra mai fu", Order = 1 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Franz Liszt",
                Works =
                [
                    new() { Name = "Hungarian Rhapsody No. 2", CatalogNumber = "S. 244/2", Movements = [new() { Name = "Lassan", Order = 1 }, new() { Name = "Friska", Order = 2 }
                        ]
                    },
                    new() { Name = "Piano Sonata in B minor", CatalogNumber = "S. 178", Movements = [new() { Name = "Lento - Allegro", Order = 1 }, new() { Name = "Andante sostenuto", Order = 2 }, new() { Name = "Allegro energico", Order = 3 }
                        ]
                    },
                    new() { Name = "Les Préludes", CatalogNumber = "S. 97", Movements = [new() { Name = "Les Preludes", Order = 1 }
                        ]
                    },
                    new() { Name = "Liebesträume No. 3", CatalogNumber = "S. 541/3", Movements = [new() { Name = "Poco allegro con affetto", Order = 1 }
                        ]
                    },
                    new() { Name = "Transcendental Études", CatalogNumber = "S. 139", Movements = [new() { Name = "No. 4 Mazeppa", Order = 1 }, new() { Name = "No. 5 Feux follets", Order = 2 }, new() { Name = "No. 11 Harmonies du soir", Order = 3 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Robert Schumann",
                Works =
                [
                    new() { Name = "Dichterliebe", CatalogNumber = "Op. 48", Movements = [new() { Name = "Im wunderschönen Monat Mai", Order = 1 }, new() { Name = "Ich grolle nicht", Order = 2 }
                        ]
                    },
                    new() { Name = "Carnaval", CatalogNumber = "Op. 9", Movements = [new() { Name = "Préambule", Order = 1 }, new() { Name = "Pierrot", Order = 2 }, new() { Name = "Arlequin", Order = 3 }, new() { Name = "Chopin", Order = 4 }
                        ]
                    },
                    new() { Name = "Piano Concerto in A minor", CatalogNumber = "Op. 54", Movements = [new() { Name = "Allegro affettuoso", Order = 1 }, new() { Name = "Intermezzo", Order = 2 }, new() { Name = "Allegro vivace", Order = 3 }
                        ]
                    },
                    new() { Name = "Symphony No. 3 in E-flat major 'Rhenish'", CatalogNumber = "Op. 97", Movements =
                        [new() { Name = "Lebhaft", Order = 1 }, new() { Name = "Scherzo", Order = 2 }, new() { Name = "Nicht schnell", Order = 3 }, new() { Name = "Feierlich", Order = 4 }, new() { Name = "Lebhaft", Order = 5 }
                        ]
                    },
                    new() { Name = "Kinderszenen", CatalogNumber = "Op. 15", Movements = [new() { Name = "Von fremden Ländern und Menschen", Order = 1 }, new() { Name = "Träumerei", Order = 2 }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Antonín Dvořák",
                Works =
                [
                    new() { Name = "Symphony No. 9 in E minor 'From the New World'", CatalogNumber = "Op. 95", Movements =
                        [new() { Name = "Adagio - Allegro", Order = 1 }, new() { Name = "Largo", Order = 2 }, new() { Name = "Scherzo", Order = 3 }, new() { Name = "Allegro con fuoco", Order = 4 }
                        ]
                    },
                    new() { Name = "Cello Concerto in B minor", CatalogNumber = "Op. 104", Movements = [new() { Name = "Allegro", Order = 1 }, new() { Name = "Adagio ma non troppo", Order = 2 }, new() { Name = "Finale", Order = 3 }
                        ]
                    },
                    new() { Name = "Slavonic Dances", CatalogNumber = "Op. 46", Movements = [new() { Name = "No. 1 in C major", Order = 1 }, new() { Name = "No. 8 in G minor", Order = 2 }
                        ]
                    },
                    new() { Name = "String Quartet No. 12 in F major 'American'", CatalogNumber = "Op. 96", Movements =
                        [new() { Name = "Allegro ma non troppo", Order = 1 }, new() { Name = "Lento", Order = 2 }, new() { Name = "Molto vivace", Order = 3 }, new() { Name = "Finale", Order = 4 }
                        ]
                    },
                    new() { Name = "Rusalka", CatalogNumber = "Op. 114", Movements = [new() { Name = "Song to the Moon", Order = 1 }
                        ]
                    }
                ]
            }
        ];
    }
}
