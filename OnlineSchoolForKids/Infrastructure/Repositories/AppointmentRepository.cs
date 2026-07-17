using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(MongoDbContext context)
        : base(context.GetCollection<Appointment>("Appointments")) { }

    // ── Existing ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<Appointment>> GetBySpecialistIdAsync(
        string specialistId, CancellationToken ct = default) =>
        await _collection
            .Find(a => a.SpecialistId == specialistId && !a.IsDeleted)
            .SortByDescending(a => a.AppointmentDate)
            .ToListAsync(ct);

    public async Task<IEnumerable<Appointment>> GetByStudentIdAsync(
        string studentId, CancellationToken ct = default) =>
        await _collection
            .Find(a => a.StudentId == studentId && !a.IsDeleted)
            .SortByDescending(a => a.AppointmentDate)
            .ToListAsync(ct);

    public async Task<IEnumerable<Appointment>> GetBookedSlotsAsync(
        string specialistId, string date, CancellationToken ct = default)
    {
        // Return slots for both Pending (held) and Confirmed — both are "taken"
        var filter = Builders<Appointment>.Filter.And(
            Builders<Appointment>.Filter.Eq(a => a.SpecialistId, specialistId),
            Builders<Appointment>.Filter.Eq(a => a.AppointmentDate, date),
            Builders<Appointment>.Filter.Eq(a => a.IsDeleted, false),
            Builders<Appointment>.Filter.In(a => a.Status,
                new[] { AppointmentStatus.Pending, AppointmentStatus.Confirmed })
        );

        return await _collection.Find(filter).ToListAsync(ct);
    }

    public async Task<bool> HasConflictAsync(
        string specialistId, string date, string startTime, string endTime,
        CancellationToken ct = default)
    {
        // A conflict exists when an active (Pending or Confirmed) appointment
        // overlaps the requested window: existing.start < requested.end
        //                             AND existing.end   > requested.start
        var filter = Builders<Appointment>.Filter.And(
            Builders<Appointment>.Filter.Eq(a => a.SpecialistId, specialistId),
            Builders<Appointment>.Filter.Eq(a => a.AppointmentDate, date),
            Builders<Appointment>.Filter.Eq(a => a.IsDeleted, false),
            Builders<Appointment>.Filter.In(a => a.Status,
                new[] { AppointmentStatus.Pending, AppointmentStatus.Confirmed }),
            Builders<Appointment>.Filter.Lt(a => a.StartTime, endTime),
            Builders<Appointment>.Filter.Gt(a => a.EndTime, startTime)
        );

        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: ct);
        return count > 0;
    }

    // ── NEW ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all Pending appointments whose 30-min hold has passed.
    /// Called by the background job every 2 minutes.
    /// </summary>
    public async Task<IEnumerable<Appointment>> GetExpiredPendingAsync(
        DateTime utcNow, CancellationToken ct = default)
    {
        var filter = Builders<Appointment>.Filter.And(
            Builders<Appointment>.Filter.Eq(a => a.IsDeleted, false),
            Builders<Appointment>.Filter.Eq(a => a.Status, AppointmentStatus.Pending),
            Builders<Appointment>.Filter.Lt(a => a.HoldExpiresAtUtc, utcNow)
        );

        return await _collection.Find(filter).ToListAsync(ct);
    }

    /// <summary>
    /// Bulk-replaces a list of appointments in one round-trip using
    /// an unordered BulkWrite of ReplaceOne operations.
    /// </summary>
    public async Task UpdateManyAsync(
        IEnumerable<Appointment> appointments, CancellationToken ct = default)
    {
        var writes = appointments
            .Select(a => new ReplaceOneModel<Appointment>(
                Builders<Appointment>.Filter.Eq(x => x.Id, a.Id), a)
            { IsUpsert = false })
            .ToList<WriteModel<Appointment>>();

        if (writes.Count > 0)
            await _collection.BulkWriteAsync(writes,
                new BulkWriteOptions { IsOrdered = false }, ct);
    }
}