namespace HospitalApi.Models;

public partial class Admission
{
    public int Id { get; set; }

    public DateTime AdmissionDate { get; set; }

    public DateTime? DischargeDate { get; set; }

    public string PatientPesel { get; set; } = null!;

    public int WardId { get; set; }

    public virtual Patient PatientPeselNavigation { get; set; } = null!;

    public virtual Ward Ward { get; set; } = null!;
}

public partial class Bed
{
    public int Id { get; set; }

    public string RoomId { get; set; } = null!;

    public int BedTypeId { get; set; }

    public virtual BedType BedType { get; set; } = null!;

    public virtual ICollection<BedAssignment> BedAssignments { get; set; } = new List<BedAssignment>();

    public virtual Room Room { get; set; } = null!;
}

public partial class BedAssignment
{
    public int Id { get; set; }

    public string PatientPesel { get; set; } = null!;

    public int BedId { get; set; }

    public DateTime From { get; set; }

    public DateTime? To { get; set; }

    public virtual Bed Bed { get; set; } = null!;

    public virtual Patient PatientPeselNavigation { get; set; } = null!;
}

public partial class BedType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();
}

public partial class Patient
{
    public string Pesel { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public int Age { get; set; }

    public bool Sex { get; set; }

    public virtual ICollection<Admission> Admissions { get; set; } = new List<Admission>();

    public virtual ICollection<BedAssignment> BedAssignments { get; set; } = new List<BedAssignment>();
}

public partial class Room
{
    public string Id { get; set; } = null!;

    public int WardId { get; set; }

    public bool HasTv { get; set; }

    public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();

    public virtual Ward Ward { get; set; } = null!;
}

public partial class Ward
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public virtual ICollection<Admission> Admissions { get; set; } = new List<Admission>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
