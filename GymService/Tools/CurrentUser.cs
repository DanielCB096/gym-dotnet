using System.Threading;

namespace GymService
{
    /// <summary>
    /// Usuario autenticado de la petición HTTP en curso.
    ///
    /// Se usa para la auditoría de GymCore: las sesiones creadas con
    /// <c>CreateSession(SubModule, CurrentUser.ForAudit)</c> rellenan
    /// CREATED_BY / LAST_UPDATED_BY con el usuario real.
    ///
    /// AsyncLocal evita fugas entre peticiones concurrentes.
    /// </summary>
    public static class CurrentUser
    {
        private static readonly AsyncLocal<long> _id = new AsyncLocal<long>();

        public static long Id
        {
            get { return _id.Value; }
            set { _id.Value = value; }
        }

        /// <summary>
        /// Valor listo para Session.UserID: el id autenticado o -1 si no hay sesión.
        /// </summary>
        public static long ForAudit
        {
            get { return _id.Value > 0 ? _id.Value : -1; }
        }
    }
}
