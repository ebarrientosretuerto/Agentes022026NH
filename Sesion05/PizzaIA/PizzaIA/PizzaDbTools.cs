using System.ComponentModel;

namespace PizzaIA;

public class PizzaDbTools
{
    private static readonly string _schema = File.ReadAllText("pizza-db.md");

    [Description(
        "Devuelve el diccionario de datos de PizzaStore. Úsala cuando el usuario pida el script SQL, " +
        "las tablas, campos, estructura o cualquier información sobre la base de datos de pizzas."
        )]
    public string ObtenerEsquema() => _schema;
}