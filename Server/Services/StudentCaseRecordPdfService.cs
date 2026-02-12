using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SharedProject;

namespace Server.Services
{
    public class StudentCaseRecordPdfService
    {
        public byte[] GenerateStudentCaseRecordPdf(SimplifiedStudentProfileCaseRecordModel record, string? schoolName = null, string? region = null, string? division = null, string? district = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Colors.Black));

                    page.Content().Column(column =>
                    {
                        column.Spacing(5);

                        // Header
                        column.Item().AlignCenter().Column(headerColumn =>
                        {
                            headerColumn.Item().Text("Republic of the Philippines").FontSize(11);
                            headerColumn.Item().Text("Department of Education").FontSize(12).Bold();
                            headerColumn.Item().PaddingTop(3).Text(schoolName ?? "SCHOOL NAME").FontSize(11).Bold();
                            headerColumn.Item().Text(region ?? "Region").FontSize(10);
                            headerColumn.Item().Text(division ?? "Division").FontSize(10);
                            headerColumn.Item().Text(district ?? "District").FontSize(10);
                        });

                        column.Item().PaddingVertical(10);

                        // Title
                        column.Item().AlignCenter().Text("STUDENT PROFILE AND CASE RECORD")
                            .FontSize(12).Bold().Underline();

                        column.Item().PaddingVertical(8);

                        // Section I: Student Profile
                        column.Item().Text("I. STUDENT PROFILE").FontSize(11).Bold();
                        column.Item().PaddingVertical(3);

                        // Student Profile Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                            });

                            // Row 1: Name, Date
                            table.Cell().Text("Name:");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Black).Text(record.RespondentName ?? "").Underline();
                            table.Cell().Text("Date:");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Black).Text(DateTime.Now.ToString("M/d/yyyy"));

                            // Row 2: Date of Birth, Age, Sex
                            table.Cell().PaddingTop(5).Text("Date of Birth:");
                            table.Cell().PaddingTop(5).BorderBottom(1).BorderColor(Colors.Black)
                                .Text(record.DateOfBirth?.ToString("M/d/yyyy") ?? "___________");
                            table.Cell().PaddingTop(5).Text("Age:");
                            table.Cell().PaddingTop(5).Row(r =>
                            {
                                r.RelativeItem().BorderBottom(1).BorderColor(Colors.Black)
                                    .Text(record.Age?.ToString() ?? "___");
                                r.ConstantItem(30).Text("  Sex:");
                                r.RelativeItem().BorderBottom(1).BorderColor(Colors.Black)
                                    .Text(record.Sex ?? "___________");
                            });

                            // Row 3: Address
                            table.Cell().PaddingTop(5).Text("Address:");
                            table.Cell().ColumnSpan(3).PaddingTop(5).BorderBottom(1).BorderColor(Colors.Black)
                                .Text(record.Address ?? "___________________________________________");

                            // Row 4: Grade & Section, Adviser
                            table.Cell().PaddingTop(5).Text("Grade & Section:");
                            table.Cell().PaddingTop(5).BorderBottom(1).BorderColor(Colors.Black)
                                .Text($"{record.GradeLevel ?? "___"} - {record.Section ?? "___"}");
                            table.Cell().PaddingTop(5).Text("Adviser:");
                            table.Cell().PaddingTop(5).BorderBottom(1).BorderColor(Colors.Black)
                                .Text(record.AdviserName ?? "___________");
                        });

                        // Parent/Guardian Info (if available)
                        if (!string.IsNullOrWhiteSpace(record.FathersName))
                        {
                            column.Item().PaddingTop(3).Row(row =>
                            {
                                row.ConstantItem(100).Text("Father's Name:");
                                row.RelativeItem().BorderBottom(1).BorderColor(Colors.Black).Text(record.FathersName);
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(record.MothersName))
                        {
                            column.Item().PaddingTop(3).Row(row =>
                            {
                                row.ConstantItem(100).Text("Mother's Name:");
                                row.RelativeItem().BorderBottom(1).BorderColor(Colors.Black).Text(record.MothersName);
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(record.GuardianName))
                        {
                            column.Item().PaddingTop(3).Row(row =>
                            {
                                row.ConstantItem(100).Text("Guardian:");
                                row.RelativeItem().BorderBottom(1).BorderColor(Colors.Black).Text(record.GuardianName);
                            });
                        }

                        column.Item().PaddingVertical(10);

                        // Section II: Case Record
                        column.Item().Text("II. CASE RECORD").FontSize(11).Bold();
                        column.Item().PaddingVertical(3);

                        // Case No and Offense No
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Row(r =>
                            {
                                r.ConstantItem(60).Text("Case No.:");
                                r.RelativeItem().BorderBottom(1).BorderColor(Colors.Black).Text("1");
                            });
                            row.RelativeItem().Row(r =>
                            {
                                r.ConstantItem(80).Text("Offense No.:");
                                r.AutoItem().Text("☐ 1st  ☐ 2nd  ☐ 3rd");
                            });
                        });

                        column.Item().PaddingTop(5);

                        // Nature of Complaint
                        column.Item().Row(row =>
                        {
                            row.ConstantItem(160).Text("Nature of Complaint/Violation:").Bold();
                            row.RelativeItem().BorderBottom(1).BorderColor(Colors.Black)
                                .Text(record.ViolationCommitted ?? "___________");
                        });

                        column.Item().PaddingTop(8);

                        // Brief Description
                        column.Item().Text("Brief Description of the Case:").Bold();
                        column.Item().Border(1).BorderColor(Colors.Black).Padding(8).MinHeight(60)
                            .Text(record.Description ?? "N/A");

                        column.Item().PaddingTop(8);

                        // Action Taken
                        column.Item().Text("Action Taken / Penalty / Agreement:").Bold();
                        column.Item().Border(1).BorderColor(Colors.Black).Padding(8).MinHeight(60)
                            .Text(record.Agreement ?? "___________");

                        column.Item().PaddingVertical(15);

                        // Signature Section
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                // Student signature
                                if (!string.IsNullOrWhiteSpace(record.StudentSignatureBase64))
                                {
                                    try
                                    {
                                        var signatureBytes = Convert.FromBase64String(record.StudentSignatureBase64);
                                        c.Item().Height(40).AlignCenter().Image(signatureBytes).FitHeight();
                                    }
                                    catch { c.Item().Height(40); }
                                }
                                else
                                {
                                    c.Item().Height(40);
                                }
                                c.Item().BorderTop(1).BorderColor(Colors.Black).PaddingTop(3)
                                    .Text("Student's Signature (Pirma)").FontSize(9).AlignCenter();
                            });

                            row.ConstantItem(20);

                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Height(40);
                                c.Item().BorderTop(1).BorderColor(Colors.Black).PaddingTop(3)
                                    .Text("Parent/Guardian's Signature").FontSize(9).AlignCenter();
                            });

                            row.ConstantItem(20);

                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Height(40);
                                c.Item().BorderTop(1).BorderColor(Colors.Black).PaddingTop(3)
                                    .Text("Adviser's Signature").FontSize(9).AlignCenter();
                            });
                        });

                        column.Item().PaddingVertical(15);

                        // POD Signature
                        column.Item().AlignCenter().Column(c =>
                        {
                            c.Item().Height(30);
                            c.Item().Width(200).BorderTop(1).BorderColor(Colors.Black).PaddingTop(3)
                                .Text("PREFECT OF DISCIPLINE").FontSize(10).Bold().AlignCenter();
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
