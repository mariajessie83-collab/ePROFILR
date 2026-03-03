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
                        headerCol.Item().AlignCenter().Column(col =>
                        {
                            col.Item().AlignCenter().Text("REPUBLIC OF THE PHILIPPINES").FontSize(10).SemiBold();
                            col.Item().AlignCenter().Text("DEPARTMENT OF EDUCATION").FontSize(10).SemiBold();
                            col.Item().AlignCenter().Text($"{region}").FontSize(10);
                            col.Item().AlignCenter().Text($"{division}").FontSize(10);
                            col.Item().AlignCenter().PaddingBottom(5).Text($"{schoolName}").FontSize(11).Bold();
                        });

                        headerCol.Item().PaddingTop(15).AlignCenter().Column(col =>
                        {
                            col.Item().AlignCenter().Text("ANNEX \"A\"").FontSize(12).Bold();
                            col.Item().AlignCenter().Text("REPORT ON INCIDENTS OF ABUSE, VIOLENCE, EXPLOITATION, DISCRIMINATION, BULLYING OR PEER ABUSE").FontSize(11).Bold();
                            col.Item().AlignCenter().Text("AND OTHER RELATED OFFENSES").FontSize(11).Bold();
                        });

                        headerCol.Item().PaddingTop(20).AlignCenter().Width(450).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Row(r => {
                                    r.AutoItem().Text("Period Covered: ").SemiBold().FontSize(10);
                                    string period = "N/A";
                                    if (startDate.HasValue && endDate.HasValue)
                                    {
                                        period = $"{startDate.Value:MMMM dd, yyyy} - {endDate.Value:MMMM dd, yyyy}";
                                    }
                                    r.RelativeItem().BorderBottom(0.5f).PaddingLeft(5).Text(period).FontSize(10);
                                });
                            });
                            row.ConstantItem(40);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Row(r => {
                                    r.AutoItem().Text("Date Generated: ").SemiBold().FontSize(10);
                                    r.RelativeItem().BorderBottom(0.5f).PaddingLeft(5).Text(DateTime.Now.ToString("MMMM dd, yyyy")).FontSize(10);
                                });
                            });
                        });
                    });

                    page.Content().PaddingTop(25).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.5f); // Victim Name
                            columns.RelativeColumn(2.5f); // Respondent Name
                            columns.ConstantColumn(35);    // Age
                            columns.ConstantColumn(35);    // Sex
                            columns.RelativeColumn(3);    // Nature of Complaint
                            columns.RelativeColumn(3.5f);  // Action Taken
                            columns.RelativeColumn(3.5f);  // Recommendation
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("VICTIM'S NAME");
                            header.Cell().Element(HeaderStyle).Text("RESPONDENT'S NAME");
                            header.Cell().Element(HeaderStyle).Text("AGE");
                            header.Cell().Element(HeaderStyle).Text("SEX");
                            header.Cell().Element(HeaderStyle).Text("NATURE OF COMPLAINT");
                            header.Cell().Element(HeaderStyle).Text("ACTION/S TAKEN");
                            header.Cell().Element(HeaderStyle).Text("RECOMMENDATION/S");
                        });

                        foreach (var record in records)
                        {
                            table.Cell().Element(CellStyle).Text(record.VictimName ?? "N/A");
                            table.Cell().Element(CellStyle).Text(record.RespondentName ?? "N/A");
                            table.Cell().Element(CellStyle).AlignCenter().Text(record.Age?.ToString() ?? "-");
                            table.Cell().Element(CellStyle).AlignCenter().Text(record.Sex ?? "-");
                            table.Cell().Element(CellStyle).Text(record.ViolationCommitted ?? "N/A");
                            table.Cell().Element(CellStyle).Text(record.ActionTaken ?? record.Agreement ?? "N/A");
                            table.Cell().Element(CellStyle).Text(record.PenaltyAction ?? "N/A");
                        }

                        // Add empty rows to reach at least 10 rows for a formal appearance
                        int remainingRows = Math.Max(0, 10 - records.Count);
                        for (int i = 0; i < remainingRows; i++)
                        {
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text("");
                            table.Cell().Element(CellStyle).Text("");
                        }
                    });

                    page.Footer().PaddingTop(50).Column(footerCol =>
                    {
                        footerCol.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Prepared by:").FontSize(10).Italic();
                                c.Item().PaddingTop(30).PaddingRight(50).Column(sig =>
                                {
                                    sig.Item().MinHeight(20).BorderBottom(0.5f).PaddingBottom(2).AlignCenter().Text("");
                                    sig.Item().AlignCenter().Text("Signature over Printed Name").FontSize(9);
                                    sig.Item().AlignCenter().Text("Designation").FontSize(9);
                                });
                            });

                            row.ConstantItem(100);

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Approved by:").FontSize(10).Italic();
                                c.Item().PaddingTop(30).PaddingRight(50).Column(sig =>
                                {
                                    sig.Item().MinHeight(20).BorderBottom(0.5f).PaddingBottom(2).AlignCenter().Text("");
                                    sig.Item().AlignCenter().Text("School Principal / School Head").FontSize(9);
                                    sig.Item().AlignCenter().Text("Date").FontSize(9);
                                });
                            });
                        });

                        footerCol.Item().PaddingTop(40).AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private IContainer HeaderStyle(IContainer container)
        {
            return container
                .DefaultTextStyle(x => x.FontSize(9).Bold())
                .PaddingVertical(8)
                .Border(0.5f)
                .BackgroundColor(Colors.Grey.Lighten4)
                .AlignCenter()
                .AlignMiddle();
        }

        private IContainer CellStyle(IContainer container)
        {
            return container
                .DefaultTextStyle(x => x.FontSize(9))
                .PaddingVertical(8)
                .PaddingHorizontal(5)
                .Border(0.5f)
                .AlignLeft()
                .AlignMiddle();
        }
    }
}
