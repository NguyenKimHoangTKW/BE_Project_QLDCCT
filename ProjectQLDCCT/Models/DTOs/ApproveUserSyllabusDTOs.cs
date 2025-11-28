namespace ProjectQLDCCT.Models.DTOs
{
    public class ApproveUserSyllabusDTOs
    {
        public int? id_ApproveUserSyllabus { get; set; }

        public int? id_syllabus { get; set; }

        public int? id_user { get; set; }

        public bool? is_approve { get; set; }

        public bool? is_key_user { get; set; }
        public int? id_course {  get; set; }
    }
}
