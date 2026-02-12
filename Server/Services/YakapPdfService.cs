using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SharedProject;

namespace Server.Services
{
    public class YakapPdfService
    {
        public byte[] GenerateYakapFormPdf(YakapFormModel yakapForm, StudentProfileCaseRecordModel? caseRecord)
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

                    page.Content().Column(column =>
                    {
                        column.Spacing(8);

                        // Header
                        column.Item().AlignCenter().Column(headerColumn =>
                        {
                            headerColumn.Item().Text("YUNIT NA AAKAY SA KABATAAN PARA SA ASAL AT PAGPAPAKATAO")
                                .FontSize(14).Bold().FontColor(Colors.Black);
                            headerColumn.Item().Text(yakapForm.SchoolName ?? "School Name")
                                .FontSize(12).SemiBold();
                            headerColumn.Item().Text("Confidential Document")
                                .FontSize(10).Italic();
                        });

                        column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);

                        // Student Information (no border/box)
                        column.Item().Padding(10).Column(infoColumn =>
                        {
                            infoColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("STUDENT NAME: ").Bold();
                                    text.Span(yakapForm.StudentName ?? caseRecord?.StudentOffenderName ?? "Not provided");
                                });
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("GRADE & SECTION: ").Bold();
                                    text.Span(yakapForm.GradeAndSection ?? 
                                        (caseRecord != null ? $"{caseRecord.GradeLevel} - {caseRecord.Section}" : "Not provided"));
                                });
                            });

                            infoColumn.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("SCHOOL NAME: ").Bold();
                                    text.Span(yakapForm.SchoolName ?? "Not provided");
                                });
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("DATE OF SESSION: ").Bold();
                                    text.Span(yakapForm.DateOfSession.ToString("MMMM dd, yyyy"));
                                });
                            });

                            infoColumn.Item().PaddingTop(5).Text(text =>
                            {
                                text.Span("FACILITATOR/COUNSELOR: ").Bold();
                                text.Span(yakapForm.FacilitatorCounselor ?? "Not provided");
                            });
                        });

                        // Bahagi I
                        column.Item().PaddingTop(10).Element(element =>
                        {
                            element.Column(sectionColumn =>
                            {
                                sectionColumn.Item().Text("Bahagi I – Pag-unawa sa Aking Karanasan")
                                    .FontSize(12).Bold().FontColor(Colors.Black);
                                sectionColumn.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Black);

                                // Question 1
                                AddQuestion(sectionColumn, 
                                    "1. Ano ang nangyari?",
                                    "(Ilarawan ang asal o sitwasyon na naging dahilan ng iyong paglahok sa Y.A.K.A.P. session)",
                                    yakapForm.Question1_AnoAngNangyari);

                                // Question 2
                                AddQuestion(sectionColumn,
                                    "2. Ano ang iniisip o nararamdaman mo noong panahong iyon?",
                                    null,
                                    yakapForm.Question2_AnoAngIniisipOFeelings);
                            });
                        });

                        // Bahagi II
                        column.Item().PaddingTop(10).Element(element =>
                        {
                            element.Column(sectionColumn =>
                            {
                                sectionColumn.Item().Text("Bahagi II – Pananagutan sa Ginawang Pagkilos")
                                    .FontSize(12).Bold().FontColor(Colors.Black);
                                sectionColumn.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Black);

                                // Question 3
                                AddQuestion(sectionColumn,
                                    "3. Ano ang naging epekto ng iyong ginawa sa iba (kaklase, guro, pamilya)?",
                                    null,
                                    yakapForm.Question3_AnoAngEpektoSaIba);

                                // Question 4
                                AddQuestion(sectionColumn,
                                    "4. Ano ang iniisip mo ngayon tungkol sa iyong mga naging desisyon?",
                                    null,
                                    yakapForm.Question4_AnoAngIniisipTungkolSaDesisyon);
                            });
                        });

                        // Bahagi III - Use compact spacing to fit all questions on one page
                        column.Item().PaddingTop(10).Element(element =>
                        {
                            element.Column(sectionColumn =>
                            {
                                sectionColumn.Item().Text("Bahagi III – Pagharap sa Hinaharap Gamit ang Positibong Disiplina")
                                    .FontSize(12).Bold().FontColor(Colors.Black);
                                sectionColumn.Item().PaddingTop(3).LineHorizontal(1).LineColor(Colors.Black);

                                // Question 5 - Use compact mode
                                AddQuestion(sectionColumn,
                                    "5. Ano ang gagawin mong iba sa susunod?",
                                    null,
                                    yakapForm.Question5_AnoAngGagawinMongIba,
                                    isCompact: true);

                                // Question 6 - Use compact mode
                                AddQuestion(sectionColumn,
                                    "6. Anong positibong pagpapahalaga o prinsipyo ang gagabay sa iyong susunod na hakbang?",
                                    "(hal. Paggagalang, Pananagutan, Katapatan, Pagtitimpi, Empatiya)",
                                    yakapForm.Question6_AnongPositibongPagpapahalaga,
                                    isCompact: true);

                                // Question 7 - Use compact mode
                                AddQuestion(sectionColumn,
                                    "7. Sumulat ng maikling mensahe para sa iyong hinaharap na sarili",
                                    "(paalala ng mga aral na natutunan mo)",
                                    yakapForm.Question7_MensaheParaSaHinaharap,
                                    isCompact: true);
                            });
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void AddQuestion(ColumnDescriptor column, string question, string? hint, string answer, bool isCompact = false)
        {
            // Wrap question in Element to prevent splitting across pages
            var topPadding = isCompact ? 5 : 10;
            column.Item().PaddingTop(topPadding).Element(element =>
            {
                element.Column(questionColumn =>
                {
                    questionColumn.Item().Text(question).Bold().FontSize(10);
                    
                    if (!string.IsNullOrEmpty(hint))
                    {
                        questionColumn.Item().PaddingTop(1).Text(hint).FontSize(9).Italic().FontColor(Colors.Grey.Darken2);
                    }

                    questionColumn.Item().PaddingTop(3).Border(1).BorderColor(Colors.Black)
                        .Padding(6).MinHeight(isCompact ? 30 : 40).Text(string.IsNullOrWhiteSpace(answer) ? "No answer provided" : answer)
                        .FontSize(10).FontColor(string.IsNullOrWhiteSpace(answer) ? Colors.Grey.Medium : Colors.Black)
                        .Italic(string.IsNullOrWhiteSpace(answer));
                });
            });
        }

        /// <summary>
        /// Generates a blank YAKAP form PDF with only student information pre-filled.
        /// The questions are left blank for manual filling.
        /// Page 1: Header, Student Info, Bahagi I, Bahagi II
        /// Page 2: Bahagi III with larger boxes
        /// </summary>
        public byte[] GenerateBlankYakapFormPdf(SimplifiedStudentProfileCaseRecordModel caseRecord, string schoolName)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Build grade/section/strand info
            var gradeSection = caseRecord.GradeLevel ?? "";
            if (!string.IsNullOrEmpty(caseRecord.Section))
            {
                gradeSection += $" - {caseRecord.Section}";
            }
            if (!string.IsNullOrEmpty(caseRecord.TrackStrand))
            {
                gradeSection += $" ({caseRecord.TrackStrand})";
            }

            var document = Document.Create(container =>
            {
                // PAGE 1: Header, Student Info, Bahagi I, Bahagi II
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial").FontColor(Colors.Black));

                    page.Content().Column(column =>
                    {
                        column.Spacing(8);

                        // Header
                        column.Item().AlignCenter().Column(headerColumn =>
                        {
                            headerColumn.Item().Text("YUNIT NA AAKAY SA KABATAAN PARA SA ASAL AT PAGPAPAKATAO")
                                .FontSize(14).Bold().FontColor(Colors.Black);
                            headerColumn.Item().Text("(Y.A.K.A.P.)")
                                .FontSize(12).Bold();
                            headerColumn.Item().Text(schoolName ?? "School Name")
                                .FontSize(12).SemiBold();
                            headerColumn.Item().Text("Confidential Document")
                                .FontSize(10).Italic();
                        });

                        column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);

                        // Student Information - VERTICAL LAYOUT (one field per line)
                        column.Item().Padding(10).Column(infoColumn =>
                        {
                            infoColumn.Item().Text(text =>
                            {
                                text.Span("STUDENT NAME: ").Bold();
                                text.Span(caseRecord.RespondentName ?? "_______________________");
                            });

                            infoColumn.Item().PaddingTop(5).Text(text =>
                            {
                                text.Span("GRADE & SECTION: ").Bold();
                                text.Span(!string.IsNullOrEmpty(gradeSection) ? gradeSection : "_______________________");
                            });

                            infoColumn.Item().PaddingTop(5).Text(text =>
                            {
                                text.Span("SCHOOL NAME: ").Bold();
                                text.Span(schoolName ?? "_______________________");
                            });

                            infoColumn.Item().PaddingTop(5).Text(text =>
                            {
                                text.Span("DATE OF SESSION: ").Bold();
                                text.Span("_______________________");
                            });

                            infoColumn.Item().PaddingTop(5).Text(text =>
                            {
                                text.Span("FACILITATOR/COUNSELOR: ").Bold();
                                text.Span("_______________________");
                            });
                        });

                        // Bahagi I
                        column.Item().PaddingTop(10).Element(element =>
                        {
                            element.Column(sectionColumn =>
                            {
                                sectionColumn.Item().Text("Bahagi I – Pag-unawa sa Aking Karanasan")
                                    .FontSize(12).Bold().FontColor(Colors.Black);
                                sectionColumn.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Black);

                                // Question 1
                                AddBlankQuestion(sectionColumn, 
                                    "1. Ano ang nangyari?",
                                    "(Ilarawan ang asal o sitwasyon na naging dahilan ng iyong paglahok sa Y.A.K.A.P. session)");

                                // Question 2
                                AddBlankQuestion(sectionColumn,
                                    "2. Ano ang iniisip o nararamdaman mo noong panahong iyon?",
                                    null);
                            });
                        });

                        // Bahagi II
                        column.Item().PaddingTop(10).Element(element =>
                        {
                            element.Column(sectionColumn =>
                            {
                                sectionColumn.Item().Text("Bahagi II – Pananagutan sa Ginawang Pagkilos")
                                    .FontSize(12).Bold().FontColor(Colors.Black);
                                sectionColumn.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Black);

                                // Question 3
                                AddBlankQuestion(sectionColumn,
                                    "3. Ano ang naging epekto ng iyong ginawa sa iba (kaklase, guro, pamilya)?",
                                    null);

                                // Question 4
                                AddBlankQuestion(sectionColumn,
                                    "4. Ano ang iniisip mo ngayon tungkol sa iyong mga naging desisyon?",
                                    null);
                            });
                        });
                    });
                });

                // PAGE 2: Bahagi III with larger boxes
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial").FontColor(Colors.Black));

                    page.Content().Column(column =>
                    {
                        column.Spacing(8);

                        // Bahagi III - Full page with larger boxes
                        column.Item().Element(element =>
                        {
                            element.Column(sectionColumn =>
                            {
                                sectionColumn.Item().Text("Bahagi III – Pagharap sa Hinaharap Gamit ang Positibong Disiplina")
                                    .FontSize(12).Bold().FontColor(Colors.Black);
                                sectionColumn.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Black);

                                // Question 5 - Large box
                                AddBlankQuestionLarge(sectionColumn,
                                    "5. Ano ang gagawin mong iba sa susunod?",
                                    null);

                                // Question 6 - Large box
                                AddBlankQuestionLarge(sectionColumn,
                                    "6. Anong positibong pagpapahalaga o prinsipyo ang gagabay sa iyong susunod na hakbang?",
                                    "(hal. Paggagalang, Pananagutan, Katapatan, Pagtitimpi, Empatiya)");

                                // Question 7 - Large box
                                AddBlankQuestionLarge(sectionColumn,
                                    "7. Sumulat ng maikling mensahe para sa iyong hinaharap na sarili",
                                    "(paalala ng mga aral na natutunan mo)");
                            });
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void AddBlankQuestion(ColumnDescriptor column, string question, string? hint, bool isCompact = false)
        {
            var topPadding = isCompact ? 5 : 10;
            column.Item().PaddingTop(topPadding).Element(element =>
            {
                element.Column(questionColumn =>
                {
                    questionColumn.Item().Text(question).Bold().FontSize(10);
                    
                    if (!string.IsNullOrEmpty(hint))
                    {
                        questionColumn.Item().PaddingTop(1).Text(hint).FontSize(9).Italic().FontColor(Colors.Grey.Darken2);
                    }

                    // Blank box for manual writing
                    questionColumn.Item().PaddingTop(3).Border(1).BorderColor(Colors.Black)
                        .Padding(6).MinHeight(isCompact ? 30 : 40).Text(" ");
                });
            });
        }

        private void AddBlankQuestionLarge(ColumnDescriptor column, string question, string? hint)
        {
            column.Item().PaddingTop(15).Element(element =>
            {
                element.Column(questionColumn =>
                {
                    questionColumn.Item().Text(question).Bold().FontSize(11);
                    
                    if (!string.IsNullOrEmpty(hint))
                    {
                        questionColumn.Item().PaddingTop(2).Text(hint).FontSize(10).Italic().FontColor(Colors.Grey.Darken2);
                    }

                    // Large blank box for manual writing
                    questionColumn.Item().PaddingTop(5).Border(1).BorderColor(Colors.Black)
                        .Padding(8).MinHeight(180).Text(" ");
                });
            });
        }
    }
}
