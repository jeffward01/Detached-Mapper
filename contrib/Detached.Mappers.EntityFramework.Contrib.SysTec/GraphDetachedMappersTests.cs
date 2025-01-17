using Detached.Mappers;
using Detached.Mappers.EntityFramework;
using Detached.Mappers.EntityFramework.Contrib.SysTec.ComplexModels;
using Detached.Mappers.EntityFramework.Contrib.SysTec.DTOs;
using GraphInheritenceTests.ComplexModels;
using GraphInheritenceTests.DeepModel;
using GraphInheritenceTests.DTOs;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace GraphInheritenceTests
{
    [TestFixture]
    public class GraphDetachedMappersTests
    {
        private Customer _superCustomer;
        private Tag _tag2;

        [SetUp]
        public void BeforeEachTest()
        {
            using (var dbContext = new ComplexDbContext())
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            }

            SeedCustomerKindsEnum();

            SeedCountry();

            var addressIngolstadt = new Address()
            {
                Street = "Hauptstraße",
                PostalCode = "85049",
                City = "Ingolstadt",
                //Country = countryDE // Problem by adding with ef as expected - must be by key
                CountryId = 1
            };

            var addressMunich = new Address()
            {
                Street = "Terminalstraße Mitte",
                PostalCode = "85445",
                City = "Oberding",
                CountryId = 1
            };

            var tag1 = new Tag() { Name = "SuperPlus" };
            _tag2 = new Tag() { Name = "Marketing Campaign1" };

            _superCustomer = new Customer()
            {
                CustomerKindId = CustomerKindId.Company,
                CustomerName = "Super Customer",
                PrimaryAddress = addressIngolstadt,
                ShipmentAddress = addressMunich,
            };
            _superCustomer.Tags.Add(tag1);
            _superCustomer.Tags.Add(_tag2);

            using (var dbContext = new ComplexDbContext())
            {
                dbContext.Customers.Add(_superCustomer);

                dbContext.SaveChanges();
            }

            // Save again with plain EF without any changes
            using (var dbContext = new ComplexDbContext())
            {
                dbContext.Update(_superCustomer);
                // No exception expected - OK
                dbContext.SaveChanges();
            }
        }

        [Test]
        public void _01_DoSimpleChangeOnAggregationCustomerKind()
        {
            DoSimpleChangeOnAggregationCustomerKind(_superCustomer);
        }

        [Test]
        public void _02_AChangeOnAggregationCountryShouldBeIgnored()
        {
            AChangeOnAggregationCountryShouldBeIgnored(_superCustomer);
        }

        [Test]
        public void _03_RemoveChangeAndAddEntriesInTagListComposition()
        {
            RemoveChangeAndAddEntriesInTagListComposition(_superCustomer, _tag2.Id);
        }

        [Test]
        public void _04_DoChangeOnCompositionOrganizationNotesWithBackReferenceOrganizationId()
        {
            DoChangeOnCompositionOrganizationNotesWithBackReferenceOrganization(_superCustomer);
        }

        [Test]
        public void _05_DoChangeOnParentChildrenTreeOrHierarchy()
        {
            DoChangeOnParentChildrenTreeOrHierarchy(); //doesn't work - parent gets lost (removed)
        }

        [Test]
        public void _06_MapOnBaseTypeLoosesConcreteTypeProperties()
        {
            var customer1 = new
            {
                OrganizationType = "Customer",
                Name = "Customer1",
                CustomerKindId = CustomerKindId.Company,
                CustomerName = "Customer1",
                PrimaryAddressId = _superCustomer.PrimaryAddressId
            };

            var government1 = new
            {
                OrganizationType = "Government",
                Name = "Government1",
                GovernmentIdentifierCode = "ABC",
                PrimaryAddressId = _superCustomer.PrimaryAddressId
            };

            using (var dbContext = new ComplexDbContext())
            {
                dbContext.Map<OrganizationBase>(customer1);   // Map<OrganizationBase> does not recognize the OrganizationType discriminator and ignores all Customer properties
                dbContext.Map<OrganizationBase>(government1); // Map<OrganizationBase> does not recognize the OrganizationType discriminator and ignores all Government properties
                dbContext.SaveChanges();
            }

            int customer1Id, government1Id;
            using (var dbContext = new ComplexDbContext())
            {
                Customer customer1Loaded = dbContext.Customers.SingleOrDefault(c => c.Name == "Customer1");
                customer1Id = customer1Loaded.Id;
                Assert.That(customer1Loaded, Is.Not.Null);
                Assert.That(customer1Loaded.CustomerName, Is.EqualTo("Customer1"), "CustomerName gets lost, because of Saving only Base type properties!");

                Government government1Loaded = dbContext.Governments.SingleOrDefault(g => g.Name == "Government1");
                government1Id = government1Loaded.Id;
                Assert.That(government1Loaded, Is.Not.Null);
                Assert.That(government1Loaded.GovernmentIdentifierCode, Is.EqualTo("ABC"));
            }

            //OrganizationNotes note1 = new OrganizationNotes() { OrganizationId = _superCustomer.Id, Date = new DateTime(2000, 05, 10), Text = "Note for Customer1" };
            //OrganizationNotes note2 = new OrganizationNotes() { OrganizationId = _superCustomer.Id, Date = new DateTime(2000, 05, 13), Text = "Note for Government1" };
            //using (var dbContext = new ComplexDbContext())
            //{
            //    note1 = dbContext.Map<OrganizationNotes>(note1);
            //    note2 = dbContext.Map<OrganizationNotes>(note2);
            //    dbContext.SaveChanges();
            //}
            //OrganizationNotes note1Loaded, note2Loaded;
            //using (var dbContext = new ComplexDbContext())
            //{
            //    note1Loaded = dbContext.OrganizationNotes.Find(note1.Id);
            //    Assert.That(note1Loaded, Is.Not.Null);

            //    note2Loaded = dbContext.OrganizationNotes.Find(note2.Id);
            //    Assert.That(note2Loaded, Is.Not.Null);
            //}

            //note1Loaded.OrganizationId = customer1Id;
            //note2Loaded.OrganizationId = government1Id;
            //using (var dbContext = new ComplexDbContext())
            //{
            //    dbContext.Map<OrganizationNotes>(note1Loaded);
            //    dbContext.Map<OrganizationNotes>(note2Loaded);
            //    dbContext.SaveChanges();
            //}
        }

        [Test]
        public void _07_AttachedExistingEntityInCompositionDoesInsertButShouldDoUpdate()
        {
            TodoItem todoItem1 = new TodoItem()
            {
                Title = "TodoItem1",
                ReusedLinkedItems = new List<ReusedLinkedItem>()
                {
                    new ReusedLinkedItem()
                    {
                        Title = "SubTodoItem1",
                        UploadedFiles = null
                    }
                }
            };

            // file will be uploaded seperately and will be inserted 
            var newFile1 = new UploadedFile() { FileTitle = "File1" };


            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                var mapped1 = dbContext.Map<TodoItem>(todoItem1);
                var mapped2 = dbContext.Map<UploadedFile>(newFile1);
                dbContext.SaveChanges();
            }


            // --------------- edit -----------------------

            var updated = new
            {
                Id = 1,
                Title = "TodoItem1",
                ReusedLinkedItems = new[]
                {
                    new
                    {
                        Id = 1,
                        Title = "SubTodoItem1",
                        UploadedFiles = new[]
                        {
                            new
                            {
                                // existing file will be added (and modified with additional properties)
                                Id = 1,
                                FileTitle = "File1 changed",
                                IsShared = true
                            }
                        }
                    }
                },
            };
            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                var mapped = dbContext.Map<TodoItem>(updated, new MapParameters { AssociateExistingCompositions = true });
                dbContext.SaveChanges();
            }

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                var todoItemLoaded = dbContext.TodoItems
                    .Include(t => t.ReusedLinkedItems)
                    .ThenInclude(s => s.UploadedFiles)
                    .First();

                Assert.That(todoItemLoaded.ReusedLinkedItems[0].UploadedFiles, Has.Count.EqualTo(1));
                Assert.That(todoItemLoaded.ReusedLinkedItems[0].UploadedFiles[0].FileTitle, Is.EqualTo("File1 changed"));
                Assert.That(todoItemLoaded.ReusedLinkedItems[0].UploadedFiles[0].IsShared, Is.True);
            }
        }

        //[Test]
        public void _07_01_AttachedExistingEntityWithReusedLinkedItemInCompositionDoesInsertButShouldDoUpdate()
        {
            User user = new()
            {
                Name = "Daniel",
                ReusedLinkedItems = new List<ReusedLinkedItem>()
                {
                    new ReusedLinkedItem()
                    {
                        Title = "SubTodoItem1",
                        UploadedFiles = null
                    }
                },
            };

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                var mapped1 = dbContext.Map<TodoItem>(user);
                dbContext.SaveChanges();
            }

            TodoItem todoItem1 = new TodoItem()
            {
                Title = "TodoItem1",
            };

            // file will be uploaded seperately and will be inserted 
            var newFile1 = new UploadedFile() { FileTitle = "File1" };


            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                var mapped1 = dbContext.Map<TodoItem>(todoItem1);
                var mapped2 = dbContext.Map<UploadedFile>(newFile1);
                dbContext.SaveChanges();
            }


            // --------------- edit -----------------------

            var updated = new
            {
                Id = 1,
                Title = "TodoItem1",
                ReusedLinkedItems = new[]
                {
                    new
                    {
                        Id = 1,
                        Title = "SubTodoItem1",
                        UploadedFiles = new[]
                        {
                            new
                            {
                                // existing file will be added (and modified with additional properties)
                                Id = 1,
                                FileTitle = "File1 changed",
                                IsShared = true
                            }
                        }
                    }
                },
                User = new
                {
                    Id = 1,
                    Name = "DanielChanged",
                    ReusedLinkedItems = new[]
                    {
                        new
                        {
                            Id = 1,
                            Title = "SubTodoItem1",
                            UploadedFiles = new[]
                            {
                                new
                                {
                                    // existing file will be added (and modified with additional properties)
                                    Id = 1,
                                    FileTitle = "File1 changed",
                                    IsShared = true
                                }
                            }
                        }
                    },
                }
            };
            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                var mapped = dbContext.Map<TodoItem>(updated, new MapParameters { AssociateExistingCompositions = true });
                //Bug: SQLite Error 19: 'UNIQUE constraint failed: SubTodoItems.Id'. Should save as update without error
                //SQL Command:
                //fail: 29.07.2022 15:19:14.409 RelationalEventId.CommandError[20102] (Microsoft.EntityFrameworkCore.Database.Command) 
                //Failed executing DbCommand(1ms)[Parameters =[@p2 = '1', @p3 = 'SubTodoItem1'(Size = 12), @p4 = '1'(Nullable = true), @p5 = '1'(Nullable = true)], CommandType = 'Text', CommandTimeout = '30']
                //INSERT INTO "ReusedLinkedItems"("Id", "Title", "TodoItemId", "UserId") //Should be update
                //VALUES(@p2, @p3, @p4, @p5);
                dbContext.SaveChanges();
            }

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                var todoItemLoaded = dbContext.TodoItems
                    .Include(t => t.User)
                    .First();

                Assert.That(todoItemLoaded.User.Name, Is.EqualTo("DanielChanged"));
            }
        }

        [Test]
        public void _08_OptionalOneToOneTriesToLoadNullId()
        {
            var austria = new Country() { IsoCode = "AT", Name = "Austria", FlagPicture = new Picture() { FileName = "rotWeissRot.png" } };
            var argentina = new Country() { IsoCode = "AR", Name = "Argentina", FlagPicture = null };

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                dbContext.Countries.Add(austria);
                dbContext.Countries.Add(argentina);
                dbContext.SaveChanges();
            }

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                IQueryable<CountryDTO> projection = dbContext.Project<Country, CountryDTO>(dbContext.Countries);

                CountryDTO austriaLoaded = projection.Single(c => c.IsoCode == "AT");
                Assert.That(austriaLoaded.Name, Is.EqualTo("Austria"));
                Assert.That(austriaLoaded.FlagPictureId, Is.GreaterThan(0));
                Assert.That(austriaLoaded.FlagPicture, Is.Not.Null);
                Assert.That(austriaLoaded.FlagPicture.FileName, Is.EqualTo("rotWeissRot.png"));

                CountryDTO argentinaLoaded = projection.Single(c => c.IsoCode == "AR"); //System.InvalidOperationException : Nullable object must have a value.
                // The origin is the empty FlagPicture - which is OK - but it tries to load a null FlagPictureId
                Assert.That(argentinaLoaded.Name, Is.EqualTo("Argentina"));
                Assert.That(argentinaLoaded.FlagPictureId, Is.Null);
                Assert.That(argentinaLoaded.FlagPicture, Is.Null);
            }
        }

        [Test]
        public void _09_MappedElementsWillBeDuplicated()
        {
            Customer customerOne = new Customer() { CustomerName = "1", PrimaryAddressId = 1 };
            Customer customerTwo = new Customer() { CustomerName = "2", PrimaryAddressId = 1 };

            int customerOneId, customerTwoId;
            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                dbContext.Customers.Add(customerOne);
                dbContext.Customers.Add(customerTwo);
                dbContext.SaveChanges();
                customerOneId = customerOne.Id;
                customerTwoId = customerTwo.Id;
            }

            var twoChangedCustomer = new
            {
                Id = customerTwoId,
                CustomerName = "2",
                //PrimaryAddressId = 1, // can be left out with DTO - will not be changed
                Recommendations = new[]
                {
                    new
                    {
                        RecommendedById = customerTwoId,
                        RecommendedToId = customerOneId,
                        RecommendationDate = new DateTime(2022, 05, 19)
                    }
                }
            };

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                var mapped = dbContext.Map<Customer>(twoChangedCustomer);

                Assert.That(mapped.Recommendations, Has.Count.EqualTo(1), "Entity reference has been duplicated, but shouldn't!");

                dbContext.SaveChanges();
            }
        }

        [Test]
        public void _10_TwoPropiertiesToSameEntityOverwriteSecondWithFirstValue()
        {
            Customer newCustomer = new Customer() { CustomerName = "no customer now", PrimaryAddressId = 1 };
            Customer fanCustomer = new Customer() { CustomerName = "fan", PrimaryAddressId = 1 };

            int newCustomerId, fanCustomerId;
            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                dbContext.Customers.Add(newCustomer);
                dbContext.Customers.Add(fanCustomer);
                dbContext.SaveChanges();
                newCustomerId = newCustomer.Id;
                fanCustomerId = fanCustomer.Id;
            }

            var fanChangedCustomer = new
            {
                Id = fanCustomerId,
                CustomerName = "fan",
                //PrimaryAddressId = 1, // can be left out with DTO - will not be changed
                Recommendations = new[]
                {
                    new
                    {
                        RecommendedById = fanCustomerId,
                        RecommendedToId = newCustomerId,
                        RecommendationDate = new DateTime(2022, 05, 19)
                    }
                }
            };

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                var mapped = dbContext.Map<Customer>(fanChangedCustomer);
                dbContext.SaveChanges();
            }

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                Customer fanLoaded = dbContext.Customers
                    .Include(c => c.Recommendations)
                    .Single(c => c.CustomerName == "fan");

                Assert.That(fanLoaded.Recommendations[0].RecommendedById, Is.EqualTo(fanCustomerId));

                Assert.That(fanLoaded.Recommendations[0].RecommendedToId, Is.EqualTo(newCustomerId), "The Id is the same as itself or RecommendedById. The changed value got lost.");
                Assert.That(fanLoaded.Recommendations[0].RecommendedToId, Is.Not.EqualTo(fanLoaded.Recommendations[0].RecommendedById));
            }
        }

        [Test]
        public void _11_ProjectionToDTOFailsIfPropertyIsNotInDTO()
        {
            var germany = new Country() { IsoCode = "DEU", Name = "Germany", FlagPicture = new Picture() { FileName = "schwarzRotGold.png" } };

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                dbContext.Countries.Add(germany);
                dbContext.SaveChanges();
            }

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                // System Null Reference Exception: 
                // We Projecting a Entity on a DTO which has not all Properties on it.
                // The type of the missing property doesnt matter (Tested with primitive, lists and complex types).
                IQueryable<CountryDTOWithoutPicture> dtosQuery = dbContext.Project<Country, CountryDTOWithoutPicture>(dbContext.Countries);
                var dto = dtosQuery.SingleOrDefault(item => item.IsoCode == "DEU");
                Assert.That(dto, Is.Not.Null);
            }
        }

        [Test]
        public void _12_MappingFailsOnManyToManyRelation()
        {
            var karen = new Student() // Karen is used later
            {
                Name = "Karen",
                Age = 22
            };

            var mike = new Student()
            {
                Name = "Mike",
                Age = 25
            };

            var math = new Course()
            {
                CourseName = "Maths",
                ClassRoomNumber = 314,
                Students = new()
            };

            math.Students.Add(mike);

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                dbContext.Courses.Add(math);
                dbContext.Students.Add(karen);
                dbContext.SaveChanges();
            }

            // Ok... Seed completed. Now a new DTO comes from the frontend (Completely unchanged).

            var mikeDTO = new StudentDTO()
            {
                Id = 1,
                Name = "Mike",
                Age = 25
            };

            var mathDTO = new CourseDTO()
            {
                Id = 1,
                CourseName = "Math",
                ClassRoomNumber = 314,
                Students = new()
            };

            mathDTO.Students.Add(mikeDTO);

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                var mapped = dbContext.Map<Course>(mathDTO);
                // on a many to many relation Detached Mappers thinks, the student is added even though thats not the case.
                Assert.DoesNotThrow(() => dbContext.SaveChanges());
            }

            // just for the sake of testing we try adding a new student

            var karenDTO = new StudentDTO()
            {
                Name = "Karen",
                Age = 22,
                Id = 2
            };

            mathDTO.Students.Add(karenDTO);

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                var mapped = dbContext.Map<Course>(mathDTO);
                Assert.That(mapped.Students.Count(), Is.EqualTo(2));
                Assert.DoesNotThrow(() => dbContext.SaveChanges());
            }

            using (ComplexDbContext dbContext = new ComplexDbContext())
            {
                var courseFromDB = dbContext.Courses.Include(c => c.Students).SingleOrDefault(c => c.Id == 1);
                Assert.That(courseFromDB.Students.Count(), Is.EqualTo(2));
            }
        }

        private static void SeedCustomerKindsEnum()
        {
            using (var dbContext = new ComplexDbContext())
            {
                foreach (CustomerKindId customerKindId in Enum.GetValues<CustomerKindId>())
                {
                    CustomerKind customerKind = new CustomerKind() { Id = customerKindId, Name = customerKindId.GetFriendlyName() };
                    dbContext.CustomerKinds.Add(customerKind);
                }

                dbContext.SaveChanges();
            }
        }

        private static void SeedCountry()
        {
            Country countryDE = new Country()
            {
                Name = "Germany",
                IsoCode = "DE",
            };

            using (var dbContext = new ComplexDbContext())
            {
                dbContext.Countries.Add(countryDE);

                dbContext.SaveChanges();
            }
        }

        private void RemoveChangeAndAddEntriesInTagListComposition(Customer superCustomer, int tag2Id)
        {
            superCustomer.Tags = new List<Tag>();
            // Tag1 removed - will not be sent back by client
            superCustomer.Tags.Add(new Tag() { Id = tag2Id, Name = "Changed Marketing Campaign1" });
            superCustomer.Tags.Add(new Tag() { Id = 0, Name = "new Tag" });

            using (var dbContext = new ComplexDbContext())
            {
                //dbContext.Update(superCustomer); // doesn't support change tracking with removed items as we know

                // Leonardo Porro suggests to use an anonymous type 
                var mapped = dbContext.Map<OrganizationBase>(new
                {
                    superCustomer.Id,
                    superCustomer.OrganizationType,
                    superCustomer.Tags
                });
                dbContext.SaveChanges();
            }
            using (var dbContext = new ComplexDbContext())
            {
                var allOrganizations = dbContext.Organizations.Include(c => c.Tags).ToList();
                Assert.That(allOrganizations, Has.Count.EqualTo(1), "Customer gets lost (removed)?!");

                Customer loadedSuperCustomer = allOrganizations.OfType<Customer>().Single(c => c.CustomerName.Contains("Super Customer"));
                Assert.That(loadedSuperCustomer.Tags, Has.Count.EqualTo(2));
                Assert.That(loadedSuperCustomer.Tags.Select(t => t.Name), Does.Not.Contains("Marketing Campaign1"));
                Assert.That(loadedSuperCustomer.Tags.Select(t => t.Name), Contains.Item("Changed Marketing Campaign1"));
                Assert.That(loadedSuperCustomer.Tags.Select(t => t.Name), Contains.Item("new Tag"));
            }
        }

        private static void DoSimpleChangeOnAggregationCustomerKind(Customer superCustomer)
        {
            superCustomer.CustomerName = "Super Customer - Changed to incomplete private";

            // the following in combination works, too :-)
            // superCustomer.CustomerKindId = CustomerKindId.Private;
            // superCustomer.CustomerKind = new CustomerKind() { Id = CustomerKindId.Private };

            superCustomer.CustomerKind = new CustomerKind() { Id = CustomerKindId.Private };

            using (var dbContext = new ComplexDbContext())
            {
                // if you use Map<Customer>(superCustomer):
                // Detached.Mappers.Exceptions.MapperException : Customer is not a valid value for discriminator in entity GraphInheritenceTests.ComplexModels.Customer.

                // base type OrganizationBase (whether it's not logical correct) works partly, but ignores the Customer specific properties.
                var mapped = dbContext.Map<Customer>(superCustomer);

                dbContext.SaveChanges();
            }

            using (var dbContext = new ComplexDbContext())
            {
                var loadedCustomer = dbContext.Customers
                    .Include(c => c.PrimaryAddress)
                    .Include(c => c.ShipmentAddress)
                    .Include(c => c.Tags)
                    .Include(c => c.CustomerKind)
                    .First();
                Assert.That(loadedCustomer.CustomerName, Is.EqualTo("Super Customer - Changed to incomplete private"), "No change would be saved. Maybe it's because it exists only in the concrete type.");
                Assert.That(loadedCustomer.CustomerKind.Id, Is.EqualTo(CustomerKindId.Private));
                Assert.That(loadedCustomer.CustomerKind.Name, Is.EqualTo("Private Customer"));
                Assert.That(loadedCustomer.PrimaryAddress.City, Is.EqualTo("Ingolstadt"));
                Assert.That(loadedCustomer.ShipmentAddress.City, Is.EqualTo("Oberding"));
                Assert.That(loadedCustomer.Tags, Has.Count.EqualTo(2));
                Assert.That(loadedCustomer.Tags.Select(t => t.Name), Contains.Item("SuperPlus"));
                Assert.That(loadedCustomer.Tags.Select(t => t.Name), Contains.Item("Marketing Campaign1"));
            }
        }

        private static void DoChangeOnCompositionOrganizationNotesWithBackReferenceOrganization(Customer superCustomer)
        {
            // Back-references don't work - OrganizationId should be included in DTO's
            superCustomer.Notes.Add(new OrganizationNotes()
            {
                Date = DateTime.Today,
                Text = "Note...",
                //OrganizationId = superCustomer.Id // is allowed in entity, but mustn't be set from Frontend
            });

            using (var dbContext = new ComplexDbContext())
            {
                superCustomer.CustomerKind = new CustomerKind { Id = CustomerKindId.Company };
                var mapped = dbContext.Map<OrganizationBase>(superCustomer);

                dbContext.SaveChanges();
            }

            using (var dbContext = new ComplexDbContext())
            {
                var superCustomerLoaded = dbContext.Customers
                    .Include(c => c.Notes)
                    .Single(c => c.Id == superCustomer.Id);

                Assert.That(superCustomerLoaded.Notes, Has.Count.EqualTo(1));
                Assert.That(superCustomerLoaded.Notes[0].Date, Is.EqualTo(DateTime.Today));
                Assert.That(superCustomerLoaded.Notes[0].Text, Is.EqualTo("Note..."));
                Assert.That(superCustomerLoaded.Notes[0].Organization.Id, Is.EqualTo(superCustomer.Id));
            }
        }

        private static void DoChangeOnParentChildrenTreeOrHierarchy()
        {
            // Seed first
            Customer parent = new Customer() { Name = "Parent", PrimaryAddressId = 1, CustomerKindId = CustomerKindId.Company };
            Customer child = new Customer() { Name = "Child", PrimaryAddressId = 1, CustomerKindId = CustomerKindId.Company };
            Customer childChild = new Customer() { Parent = child, Name = "ChildChild", PrimaryAddressId = 1, CustomerKindId = CustomerKindId.Company };
            child.Children.Add(childChild);
            using (var dbContext = new ComplexDbContext())
            {
                var mapped1 = dbContext.Add<Customer>(parent);
                var mapped2 = dbContext.Add<Customer>(child);
                var mapped3 = dbContext.Add<Customer>(childChild);

                dbContext.SaveChanges();
            }

            child.ParentId = parent.Id;
            // Link tree as aggregation
            //child.Parent = new Customer { Id = parent.Id };
            parent.Children.Add(child);

            using (var dbContext = new ComplexDbContext())
            {
                //dbContext.Update(parent); // works - expected
                //dbContext.Update(child); // works - expected

                // if the whole entities are used - parent gets lost (removed)
                // so the workaround with anonymous type is working
                var mapped1 = dbContext.Map<OrganizationBase>(new
                {
                    parent.Id,
                    parent.OrganizationType,
                    parent.ParentId,
                    parent.Parent,
                    parent.Children
                });
                dbContext.SaveChanges();

                var mapped2 = dbContext.Map<OrganizationBase>(new
                {
                    child.Id,
                    child.OrganizationType,
                    child.ParentId,
                    child.Parent,
                    child.Children
                });
                dbContext.SaveChanges();
            }

            using (var dbContext = new ComplexDbContext())
            {
                var allCustomers = dbContext.Customers;
                var loadedHierarchy = allCustomers
                    .Include(c => c.Parent)
                    .Include(c => c.Children)
                    .AsEnumerable();

                Assert.That(loadedHierarchy.Select(c => c.Name), Contains.Item("Parent"), "Parent gets lost (removed)?!");
                Customer parentLoaded = loadedHierarchy.Single(c => c.Name == "Parent");

                Assert.That(parentLoaded.Children, Has.Count.EqualTo(1));
                Assert.That(parentLoaded.Children[0].Name, Is.EqualTo("Child"));
                Assert.That(parentLoaded.Children[0].Children, Has.Count.EqualTo(1));
                Assert.That(parentLoaded.Children[0].Children[0].Name, Is.EqualTo("ChildChild"));
            }
        }

        private static void AChangeOnAggregationCountryShouldBeIgnored(Customer superCustomer)
        {
            superCustomer.PrimaryAddress = new Address() { Id = 1, Country = new Country() { Name = "changed" } };

            using (var dbContext = new ComplexDbContext())
            {
                // preferred suggested way
                var mapped = dbContext.Map<OrganizationBase>(new
                {
                    superCustomer.Id,
                    superCustomer.OrganizationType,
                    superCustomer.PrimaryAddress
                });

                // works, too
                // var mapped = dbContext.Map<OrganizationBase>(superCustomer);

                dbContext.SaveChanges();
            }

            using (var dbContext = new ComplexDbContext())
            {
                Assert.That(dbContext.Countries.Count, Is.EqualTo(1));

                var germanyReLoaded = dbContext.Countries.First();
                // country as aggregate shouldn't be changed
                Assert.That(germanyReLoaded.Name, Is.Not.EqualTo("changed"));
                Assert.That(germanyReLoaded.Name, Is.EqualTo("Germany"));
            }
        }

        //[Test]
        public void _13_UpdateConcreteTypeLoadsOnlyBaseType()
        {
            var organizationList = new OrganizationListDTO()
            {
                ListName = "Europe",
            };

            OrganizationListDTO loaded;

            using (var dbContext = new ComplexDbContext())
            {
                dbContext.Map<OrganizationList>(organizationList);
                dbContext.SaveChanges();

                var query = dbContext.OrganizationLists.Include(o => o.Organizations);

                var projected = dbContext.Project<OrganizationList, OrganizationListDTO>(query);

                loaded = projected.First();

            }

            // Organizations is type of OrganisationBase
            // It inherits like this: OrganisationBase -> Government -> GovernmentLeader
            loaded.Organizations.Add(new GovernmentLeaderDTO()
            {
                GovernmentIdentifierCode = "DE",
                OrganizationType = nameof(GovernmentLeader),
                PrimaryAddressId = 1,
                Name = "Germany",
                LeaderName = "Olaf Scholz",
            });

            loaded.Organizations.Add(new GovernmentLeaderDTO()
            {
                GovernmentIdentifierCode = "AU",
                OrganizationType = nameof(GovernmentLeader),
                PrimaryAddressId = 1,
                Name = "Austria",
                LeaderName = "Alexander Van der Bellen",
            });

            OrganizationListDTO loaded2;
            using (var dbContext = new ComplexDbContext())
            {
                dbContext.Map<OrganizationList>(loaded);
                dbContext.SaveChanges();

                var query = dbContext.OrganizationLists.Include(o => o.Organizations);
                var projected = dbContext.Project<OrganizationList, OrganizationListDTO>(query);
                loaded2 = projected.First();

            }

            // The problem is that the project method always returns the base type and not
            // the original type (GovermentLeaderDTO) and I understand why this happens... 
            // the mapper has no chance to find out the right type of the dto purely based on the discriminator value.
            // Maybe we could specify it when setting up Detached Mappers when declaring the discriminator or as parameter
            // of the project method? Do you have an idea? Or another solution?
            Assert.That(((GovernmentLeaderDTO)loaded2.Organizations[0]).LeaderName, Is.EqualTo("Olaf Scholz"));
        }
    }
}