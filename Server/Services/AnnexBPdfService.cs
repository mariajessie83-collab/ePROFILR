using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SharedProject;

namespace Server.Services
{
    public class AnnexBPdfService
    {
        public byte[] GenerateAnnexBPdf(SimplifiedStudentProfileCaseRecordModel record)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial").FontColor(Colors.Black));

                    page.Header().AlignRight().Text("Annex \"B\"").FontSize(12).Bold();

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        // Title
                        column.Item().AlignCenter().Column(titleColumn =>
                        {
                            titleColumn.Item().AlignCenter().Text("Department of Education".ToUpper()).FontSize(13).Bold();
                            titleColumn.Item().AlignCenter().Text("Intake Sheet".ToUpper()).FontSize(13).Bold().Underline();
                        });

                        column.Item().PaddingTop(10).Text("I. INFORMATION:").FontSize(12).Bold();

                        // Victim Section
                        column.Item().PaddingLeft(20).Column(victimColumn =>
                        {
                            victimColumn.Item().Text("A. VICTIM:").Bold();
                            
                            victimColumn.Item().Row(row =>
                            {
                                row.ConstantItem(50).Text("Name: ");
                                row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text(record.VictimName ?? "No Specific Victim");
                            });

                            victimColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Row(r => {
                                    r.AutoItem().Text("Date of Birth: ");
                                    r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                });
                                row.ConstantItem(10);
                                row.RelativeItem(0.5f).Row(r => {
                                    r.AutoItem().Text("Age: ");
                                    r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                });
                                row.ConstantItem(10);
                                row.RelativeItem(0.5f).Row(r => {
                                    r.AutoItem().Text("Sex: ");
                                    r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                });
                            });

                            victimColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Row(r => {
                                    r.AutoItem().Text("Gr./Yr and Section: ");
                                    r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                });
                                row.ConstantItem(10);
                                row.RelativeItem().Row(r => {
                                    r.AutoItem().Text("Adviser: ");
                                    r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                });
                            });

                            bool hasVictimMother = false; // We don't have victim parent data yet
                            bool hasVictimFather = false;
                            
                            if (hasVictimMother || hasVictimFather)
                            {
                                victimColumn.Item().PaddingTop(5).Text("Parents:");
                                victimColumn.Item().PaddingLeft(20).Column(parentColumn =>
                                {
                                    if (hasVictimMother)
                                    {
                                        parentColumn.Item().Row(row => {
                                            row.RelativeItem(2).Row(r => {
                                                r.AutoItem().Text("Mother: ");
                                                r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                            });
                                            row.ConstantItem(10);
                                            row.RelativeItem().Row(r => {
                                                r.AutoItem().Text("Age: ");
                                                r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                            });
                                        });
                                        parentColumn.Item().Row(row => {
                                            row.AutoItem().Text("Occupation: ");
                                            row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                        });
                                        parentColumn.Item().Row(row => {
                                            row.AutoItem().Text("Address: ");
                                            row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                        });
                                    }

                                    if (hasVictimFather)
                                    {
                                        parentColumn.Item().PaddingTop(5).Row(row => {
                                            row.RelativeItem(2).Row(r => {
                                                r.AutoItem().Text("Father: ");
                                                r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                            });
                                            row.ConstantItem(10);
                                            row.RelativeItem().Row(r => {
                                                r.AutoItem().Text("Age: ");
                                                r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                            });
                                        });
                                        parentColumn.Item().Row(row => {
                                            row.AutoItem().Text("Occupation: ");
                                            row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                        });
                                    }
                                });
                            }
                            
                            victimColumn.Item().Row(row => {
                                row.AutoItem().Text("Address and Contact Number: ");
                                row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text(record.VictimContact ?? "");
                            });
                        });

                        // Complainant Section
                        column.Item().PaddingLeft(20).Column(complainantColumn =>
                        {
                            complainantColumn.Item().PaddingTop(10).Text("B. COMPLAINANT:").Bold();
                            complainantColumn.Item().Row(row => {
                                row.AutoItem().Text("Name: ");
                                row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text(record.ComplainantName ?? "");
                            });
                            complainantColumn.Item().Row(row => {
                                row.AutoItem().Text("Relationship to Victim: ");
                                row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                            });
                            complainantColumn.Item().Row(row => {
                                row.AutoItem().Text("Address and Contact Number: ");
                                row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text(record.ComplainantContact ?? "");
                            });
                        });

                        // Respondent Section
                        column.Item().PaddingLeft(20).Column(respondentColumn =>
                        {
                            respondentColumn.Item().PaddingTop(10).Text("C. RESPONDENT:").Bold();
                            
                            respondentColumn.Item().PaddingBottom(5).Text("C-1. If respondent is a School Personnel").Italic();
                            respondentColumn.Item().Row(row => {
                                row.AutoItem().Text("Name: ");
                                row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                            });
                            // ... omitting other C-1 subfields for brevity as per existing logic ...

                            respondentColumn.Item().PaddingTop(10).Text("C-2. If respondent is a Student").Italic();
                            respondentColumn.Item().Row(row => {
                                row.AutoItem().Text("Name: ");
                                row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text(record.RespondentName ?? "");
                            });
                            respondentColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Row(r => {
                                    r.AutoItem().Text("Date of Birth: ");
                                    r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text(record.DateOfBirth?.ToString("MMMM dd, yyyy") ?? "");
                                });
                                row.ConstantItem(10);
                                row.RelativeItem(0.5f).Row(r => {
                                    r.AutoItem().Text("Age: ");
                                    r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text(record.Age?.ToString() ?? "");
                                });
                                row.ConstantItem(10);
                                row.RelativeItem(0.5f).Row(r => {
                                    r.AutoItem().Text("Sex: ");
                                    r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text(record.Sex ?? "");
                                });
                            });

                            respondentColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Row(r => {
                                    r.AutoItem().Text("Gr./Yr and Section: ");
                                    r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text($"{record.GradeLevel ?? ""} - {record.Section ?? ""}");
                                });
                                row.ConstantItem(10);
                                row.RelativeItem().Row(r => {
                                    r.AutoItem().Text("Adviser: ");
                                    r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text(record.AdviserName ?? "");
                                });
                            });

                            bool hasResMother = !string.IsNullOrWhiteSpace(record.MothersName);
                            bool hasResFather = !string.IsNullOrWhiteSpace(record.FathersName);
                            bool hasResGuardian = !string.IsNullOrWhiteSpace(record.GuardianName);

                            if (hasResMother || hasResFather || hasResGuardian)
                            {
                                respondentColumn.Item().PaddingTop(5).Text("Parents/Guardian:");
                                respondentColumn.Item().PaddingLeft(20).Column(pCol => {
                                    if (hasResMother)
                                    {
                                        pCol.Item().Row(row => {
                                            row.RelativeItem(2).Row(r => {
                                                r.AutoItem().Text("Mother: ");
                                                r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text(record.MothersName ?? "");
                                            });
                                            row.ConstantItem(10);
                                            row.RelativeItem().Row(r => {
                                                r.AutoItem().Text("Age: ");
                                                r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                            });
                                        });
                                        pCol.Item().Row(row => {
                                            row.AutoItem().Text("Occupation: ");
                                            row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                        });
                                        pCol.Item().Row(row => {
                                            row.AutoItem().Text("Address and Contact Number: ");
                                            row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                        });
                                    }

                                    if (hasResFather)
                                    {
                                        pCol.Item().PaddingTop(hasResMother ? 5 : 0).Row(row => {
                                            row.RelativeItem(2).Row(r => {
                                                r.AutoItem().Text("Father: ");
                                                r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text(record.FathersName ?? "");
                                            });
                                            row.ConstantItem(10);
                                            row.RelativeItem().Row(r => {
                                                r.AutoItem().Text("Age: ");
                                                r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                            });
                                        });
                                        pCol.Item().Row(row => {
                                            row.AutoItem().Text("Occupation: ");
                                            row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                        });
                                        pCol.Item().Row(row => {
                                            row.AutoItem().Text("Address and Contact Number: ");
                                            row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                        });
                                    }

                                    if (hasResGuardian)
                                    {
                                        pCol.Item().PaddingTop((hasResMother || hasResFather) ? 5 : 0).Row(row => {
                                            row.RelativeItem(2).Row(r => {
                                                r.AutoItem().Text("Guardian: ");
                                                r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text(record.GuardianName ?? "");
                                            });
                                            row.ConstantItem(10);
                                            row.RelativeItem().Row(r => {
                                                r.AutoItem().Text("Age: ");
                                                r.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                            });
                                        });
                                        pCol.Item().Row(row => {
                                            row.AutoItem().Text("Occupation: ");
                                            row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                        });
                                        pCol.Item().Row(row => {
                                            row.AutoItem().Text("Address and Contact Number: ");
                                            row.RelativeItem().BorderBottom(1).PaddingBottom(2).Text("");
                                        });
                                    }
                                });
                            }
                        });

                        // Details of the Case
                        column.Item().PaddingTop(10).Text("II. DETAILS OF THE CASE:").FontSize(12).Bold();
                        column.Item().PaddingLeft(20).Column(descCol => 
                        {
                            var description = record.Description ?? "";
                            var lines = SplitIntoLines(description, 90);
                            foreach (var line in lines.Take(5))
                            {
                                descCol.Item().PaddingBottom(2).BorderBottom(1).MinHeight(15).Text(line);
                            }
                            for (int i = lines.Count(); i < 5; i++)
                            {
                                descCol.Item().PaddingBottom(2).BorderBottom(1).MinHeight(15).Text(" ");
                            }
                        });

                        // Action Taken
                        column.Item().PaddingTop(10).Text("III. ACTION TAKEN:").FontSize(12).Bold();
                        column.Item().PaddingLeft(20).Column(actionCol => 
                        {
                            var agreement = record.Agreement ?? "";
                            var lines = SplitIntoLines(agreement, 90);
                            foreach (var line in lines.Take(4))
                            {
                                actionCol.Item().PaddingBottom(2).BorderBottom(1).MinHeight(15).Text(line);
                            }
                            for (int i = lines.Count(); i < 4; i++)
                            {
                                actionCol.Item().PaddingBottom(2).BorderBottom(1).MinHeight(15).Text(" ");
                            }
                        });

                        // Recommendations
                        column.Item().PaddingTop(10).Text("IV. RECOMMENDATIONS:").FontSize(12).Bold();
                        column.Item().PaddingLeft(20).Column(recCol => 
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                recCol.Item().PaddingBottom(2).BorderBottom(1).MinHeight(15).Text(" ");
                            }
                        });

                        // Preparation
                        column.Item().PaddingTop(20).Row(row =>
                        {
                            row.RelativeItem();
                            row.ConstantItem(250).Column(prepCol =>
                            {
                                prepCol.Item().Text("Prepared by:").Bold();
                                prepCol.Item().PaddingTop(10).BorderBottom(1).AlignCenter().Text("___________________________").Bold();
                                prepCol.Item().AlignCenter().Text("Name over Printed Name").FontSize(9);
                                
                                prepCol.Item().PaddingTop(5).BorderBottom(1).AlignCenter().Text("PREFECT OF DISCIPLINE").Bold();
                                prepCol.Item().AlignCenter().Text("Designation").FontSize(9);

                                prepCol.Item().PaddingTop(5).BorderBottom(1).AlignCenter().Text(DateTime.Now.ToString("MM/dd/yyyy"));
                                prepCol.Item().AlignCenter().Text("Date").FontSize(9);
                            });
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private List<string> SplitIntoLines(string text, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();
            var lines = new List<string>();
            for (int i = 0; i < text.Length; i += maxCharsPerLine)
            {
                lines.Add(text.Substring(i, Math.Min(maxCharsPerLine, text.Length - i)));
            }
            return lines;
        }
    }
}
