using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SharedProject;

namespace Server.Services
{
    public class AnnexAPdfService
    {
        public byte[] GenerateAnnexAPdf(List<SimplifiedStudentProfileCaseRecordModel> records, string schoolName = "", string division = "", string region = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Colors.Black));

                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text("XIV. CHILD PROTECTION POLICY FORMS").FontSize(12).Bold();
                            row.AutoItem().Text("Annex \"A\"").FontSize(12).Bold();
                        });
                        
                        headerCol.Item().PaddingTop(5).AlignCenter().Text("Report of cases of abuse, violence, exploitation, discrimination, bullying or peer abuse and other related offenses").FontSize(11).Bold();

                        headerCol.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Row(r => {
                                    r.AutoItem().Text("School/Division/Region: ");
                                    r.RelativeItem().BorderBottom(0.5f).Text($"{schoolName} / {division} / {region}");
                                });
                                c.Item().Row(r => {
                                    r.AutoItem().Text("Period Covered: ");
                                    string period = "";
                                    if (startDate.HasValue && endDate.HasValue)
                                    {
                                        if (startDate.Value.Month == endDate.Value.Month && startDate.Value.Year == endDate.Value.Year)
                                        {
                                            period = startDate.Value.ToString("MMMM yyyy");
                                        }
                                        else
                                        {
                                            period = $"{startDate.Value:MM/dd/yyyy} - {endDate.Value:MM/dd/yyyy}";
                                        }
                                    }
                                    else if (startDate.HasValue)
                                    {
                                        period = $"From {startDate.Value:MM/dd/yyyy}";
                                    }
                                    else if (endDate.HasValue)
                                    {
                                        period = $"Until {endDate.Value:MM/dd/yyyy}";
                                    }
                                    r.RelativeItem().BorderBottom(0.5f).Text(period);
                                });
                                c.Item().Row(r => {
                                    r.AutoItem().Text("Person Submitting Report: ");
                                    r.RelativeItem().BorderBottom(0.5f).Text(""); // Placeholder
                                });
                            });
                            row.ConstantItem(40);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().PaddingTop(20).Row(r => {
                                    r.AutoItem().Text("Designation: ");
                                    r.RelativeItem().BorderBottom(0.5f).Text(""); // Placeholder
                                });
                                c.Item().Row(r => {
                                    r.AutoItem().Text("Date: ");
                                    r.RelativeItem().BorderBottom(0.5f).Text(DateTime.Now.ToString("MM/dd/yyyy"));
                                });
                            });
                        });
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        // 7 Column definitions
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Victim Name
                            columns.RelativeColumn(2); // Respondent Name
                            columns.ConstantColumn(45); // Respondent Age (Increased from 35)
                            columns.ConstantColumn(45); // Respondent Sex (Increased from 35)
                            columns.RelativeColumn(3); // Nature of Complaint (Increased from 2)
                            columns.RelativeColumn(3); // Action/s Taken (Increased from 2)
                            columns.RelativeColumn(3); // Recommendation/s (Increased from 2)
                        });

                        // Header rows
                        table.Header(header =>
                        {
                            // Col 1: Victim
                            header.Cell().RowSpan(2).Element(HeaderStyle).Text("VICTIM/S\n\nNAME");
                            
                            // Col 2-4: Respondent
                            header.Cell().ColumnSpan(3).Element(HeaderStyle).Text("RESPONDENT/S");
                            
                            // Col 5: Nature
                            header.Cell().RowSpan(2).Element(HeaderStyle).Text("NATURE OF COMPLAINT");
                            // Col 6: Action
                            header.Cell().RowSpan(2).Element(HeaderStyle).Text("ACTION/S TAKEN");
                            // Col 7: Recommendation
                            header.Cell().RowSpan(2).Element(HeaderStyle).Text("RECOMMENDATION/S");

                            // Row 2 sub-headers for Respondent only
                            header.Cell().Element(HeaderSubStyle).Text("NAME");
                            header.Cell().Element(HeaderSubStyle).Text("AGE");
                            header.Cell().Element(HeaderSubStyle).Text("SEX (M or F)");
                        });

                        // Data rows
                        foreach (var record in records)
                        {
                            // Clean Victim Name: remove trailing (Age, Sex) or similar info if present
                            string cleanedVictimName = record.VictimName ?? "";
                            if (cleanedVictimName.Contains("("))
                            {
                                int parenIndex = cleanedVictimName.IndexOf("(");
                                cleanedVictimName = cleanedVictimName.Substring(0, parenIndex).Trim();
                            }
                            
                            table.Cell().Element(CellStyle).Text(cleanedVictimName);
                            
                            table.Cell().Element(CellStyle).Text(record.RespondentName ?? "");
                            table.Cell().Element(CellStyle).AlignCenter().Text(record.Age?.ToString() ?? "");
                            table.Cell().Element(CellStyle).AlignCenter().Text(record.Sex ?? "");
                            
                            table.Cell().Element(CellStyle).Text(record.ViolationCommitted ?? "");
                            table.Cell().Element(CellStyle).Text(record.ActionTaken ?? record.Agreement ?? "");
                            table.Cell().Element(CellStyle).Text(record.PenaltyAction ?? "");
                        }

                    });
                });
            });

            return document.GeneratePdf();
        }

        private IContainer HeaderStyle(IContainer container)
        {
            return container
                .DefaultTextStyle(x => x.FontSize(10).Bold())
                .Padding(3)
                .Border(0.5f)
                .AlignCenter()
                .AlignMiddle();
        }

        private IContainer HeaderSubStyle(IContainer container)
        {
            return container
                .DefaultTextStyle(x => x.FontSize(9).Bold())
                .Padding(2)
                .Border(0.5f)
                .AlignCenter()
                .AlignMiddle();
        }

        private IContainer CellStyle(IContainer container)
        {
            return container
                .DefaultTextStyle(x => x.FontSize(10))
                .PaddingVertical(4)
                .PaddingHorizontal(8)
                .MinHeight(25)
                .Border(0.5f)
                .AlignLeft()
                .AlignMiddle();
        }
    }
}
