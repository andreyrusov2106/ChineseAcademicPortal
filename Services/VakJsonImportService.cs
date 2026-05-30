using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ChineseAcademicPortal;
using ChineseAcademicPortal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChineseAcademicPortal.Services
{
    public class VakJsonImportService
    {
        private readonly HttpClient _http;
        private readonly AppDbContext _db;
        private readonly ILogger<VakJsonImportService> _logger;

        private const string VakJsonUrl = "https://journalrank.rcsi.science/ru/record-sources/download/?dataType=Json";

        public VakJsonImportService(HttpClient http, AppDbContext db, ILogger<VakJsonImportService> logger)
        {
            _http = http;
            _db = db;
            _logger = logger;
        }

        public async Task<int> ImportFromJsonAsync()
        {
            try
            {
                _logger.LogInformation("📥 Downloading journals JSON...");
                var json = await _http.GetStringAsync(VakJsonUrl);
                _logger.LogInformation("📄 Downloaded {Length} bytes. Parsing...", json.Length);

                var journals = JsonSerializer.Deserialize<List<VakJournalDto>>(json);
                if (journals == null || journals.Count == 0)
                {
                    _logger.LogWarning("⚠️ Empty JSON or parsing failed");
                    return 0;
                }

                _logger.LogInformation("🔍 Found {Count} records. Filtering RSCI/ВАК journals...", journals.Count);

                var updated = 0;
                foreach (var dto in journals)
                {
                    // Берём первый ISSN и название
                    var issn = dto.issns?.FirstOrDefault();
                    var name = dto.title?.FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(issn) || string.IsNullOrWhiteSpace(name))
                        continue;

                    // 🔍 Фильтр: оставляем только журналы, индексированные в РИНЦ (ВАК)
                    var isVak = dto.rsci?.value == true;
                    if (!isVak) continue;

                    // Формируем примечание по индексации
                    var noteParts = new List<string> { "ВАК/РИНЦ" };
                    if (dto.scopus?.value == true) noteParts.Add("Scopus");
                    if (dto.wos_cc?.value == true) noteParts.Add("WoS");
                    var note = string.Join(", ", noteParts);

                    // Поиск существующей записи по ISSN
                    var existing = await _db.VakJournals
                        .FirstOrDefaultAsync(j => j.Issn == issn);

                    if (existing != null)
                    {
                        existing.NameRu = name;
                        existing.IsActive = true;
                        existing.Note = note;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                    else
                    {
                        await _db.VakJournals.AddAsync(new VakJournal
                        {
                            NameRu = name,
                            Issn = issn,
                            IsActive = true,
                            Note = note,
                            Url = "", // Ссылки нет в JSON, оставим пустым
                            Category = "Не определена",
                            UpdatedAt = DateTime.UtcNow
                        });
                        updated++;
                    }

                    // Пакетное сохранение каждые 1000 записей (экономит память)
                    if (updated % 1000 == 0)
                    {
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("💾 Saved batch of 1000...");
                    }
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("✅ Import finished. Added/Updated: {Count} VAK journals", updated);
                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Import failed");
                throw;
            }
        }
    }
}