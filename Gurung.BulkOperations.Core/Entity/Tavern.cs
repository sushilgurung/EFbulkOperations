using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations.Core.Entity
{
    /// <summary>
    /// Tavern entity with [Column] attributes mapping C# properties to snake_case database columns.
    /// This tests that bulk operations properly respect EF Core column name mappings.
    /// </summary>
    [Table("taverns")]
    public sealed class Tavern
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("user_id")]
        [MaxLength(450)]
        public required string UserId { get; set; }

        [Column("owner_name")]
        [MaxLength(255)]
        public string? OwnerName { get; set; }

        [Column("country_id")]
        public long CountryId { get; set; }

        [Column("gender")]
        [MaxLength(255)]
        public string? Gender { get; set; }

        [Column("tavern_name")]
        [MaxLength(255)]
        public string? TavernName { get; set; }

        [Column("street_address")]
        [MaxLength(255)]
        public string? StreetAddress { get; set; }

        [Column("city")]
        [MaxLength(255)]
        public string? City { get; set; }

        [Column("state_id")]
        public int StateId { get; set; }

        [Column("zip_code")]
        [MaxLength(20)]
        public string? ZipCode { get; set; }

        [Column("location_id")]
        public long LocationId { get; set; }

        [Column("county")]
        [MaxLength(200)]
        public string? County { get; set; }

        [Column("phone")]
        [MaxLength(20)]
        public string? Phone { get; set; }

        [Column("fax")]
        [MaxLength(20)]
        public string? Fax { get; set; }

        [Column("website_url")]
        [MaxLength(255)]
        public string? WebsiteUrl { get; set; }

        [Column("first_name")]
        [MaxLength(255)]
        public string? FirstName { get; set; }

        [Column("last_name")]
        [MaxLength(255)]
        public string? LastName { get; set; }

        [Column("user_name")]
        [MaxLength(255)]
        public required string UserName { get; set; }

        [Column("password")]
        [MaxLength(255)]
        public required string Password { get; set; }

        [Column("email")]
        [MaxLength(255)]
        public required string Email { get; set; }

        [Column("cell_phone")]
        [MaxLength(20)]
        public string? CellPhone { get; set; }

        [Column("big_brain_first_name")]
        [MaxLength(255)]
        public string? BigBrainFirstName { get; set; }

        [Column("big_brain_last_name")]
        [MaxLength(255)]
        public string? BigBrainLastName { get; set; }

        [Column("big_brain_email")]
        [MaxLength(255)]
        public string? BigBrainEmail { get; set; }

        [Column("big_brain_cell")]
        [MaxLength(255)]
        public string? BigBrainCell { get; set; }

        [Column("max_table")]
        public int? MaxTable { get; set; }

        [Column("first_night")]
        [MaxLength(45)]
        public string? FirstNight { get; set; }

        [Column("first_night_time1")]
        [MaxLength(45)]
        public string? FirstNightTime1 { get; set; }

        [Column("first_night_time2")]
        [MaxLength(45)]
        public string? FirstNightTime2 { get; set; }

        [Column("second_night")]
        [MaxLength(45)]
        public string? SecondNight { get; set; }

        [Column("second_night_time1")]
        [MaxLength(45)]
        public string? SecondNightTime1 { get; set; }

        [Column("second_night_time2")]
        [MaxLength(45)]
        public string? SecondNightTime2 { get; set; }

        [Column("check_in_time")]
        [MaxLength(45)]
        public string? CheckInTime { get; set; }

        [Column("registration_closes")]
        [MaxLength(45)]
        public string? RegistrationCloses { get; set; }

        [Column("registration_opens")]
        [MaxLength(45)]
        public string? RegistrationOpens { get; set; }

        [Column("max_no_of_teams")]
        public long MaxNumberOfTeams { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("time_zone")]
        [MaxLength(25)]
        public string? TimeZone { get; set; }

        [Column("activity")]
        public DateTime Activity { get; set; }

        [Column("no_of_time_update")]
        public int? NumberOfTimeUpdate { get; set; }

        [Column("owner_id")]
        public long OwnerId { get; set; }

        [Column("notes")]
        [MaxLength(1500)]
        public string? Notes { get; set; }

        [Column("avg_teams")]
        public int? AverageTeams { get; set; }

        [Column("bonus_qs")]
        public int? BonusQuestions { get; set; }

        [Column("birth_date")]
        public DateTime? BirthDate { get; set; }

        [Column("players_per_table")]
        public int? PlayersPerTable { get; set; }

        [Column("big_brain_first_name2")]
        [MaxLength(45)]
        public string? BigBrainFirstName2 { get; set; }

        [Column("big_brain_email_2")]
        [MaxLength(45)]
        public string? BigBrainEmail2 { get; set; }

        [Column("big_brain_last_name2")]
        [MaxLength(45)]
        public string? BigBrainLastName2 { get; set; }

        [Column("big_brain_cell2")]
        [MaxLength(45)]
        public string? BigBrainCell2 { get; set; }

        [Column("weekly_first_prize")]
        [MaxLength(300)]
        public string? WeeklyFirstPrize { get; set; }

        [Column("weekly_second_prize")]
        [MaxLength(300)]
        public string? WeeklySecondPrize { get; set; }

        [Column("weekly_third_prize")]
        [MaxLength(300)]
        public string? WeeklyThirdPrize { get; set; }

        [Column("weekly_specials")]
        [MaxLength(300)]
        public string? WeeklySpecials { get; set; }

        [Column("tv_version")]
        public bool TvVersion { get; set; }

        [Column("shipping_address")]
        [MaxLength(45)]
        public string? ShippingAddress { get; set; }

        [Column("shipping_city")]
        [MaxLength(45)]
        public string? ShippingCity { get; set; }

        [Column("shipping_state_id")]
        public int? ShippingStateId { get; set; }

        [Column("shipping_zip")]
        [MaxLength(45)]
        public string? ShippingZip { get; set; }

        [Column("autogame_mode")]
        public int AutoGameMode { get; set; }

        [Column("food_special")]
        [MaxLength(45)]
        public string? FoodSpecial { get; set; }

        [Column("drink_special")]
        [MaxLength(45)]
        public string? DrinkSpecial { get; set; }

        [Column("chk_ok")]
        public bool ChkOk { get; set; }
    }
}
