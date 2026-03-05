/*    
   Copyright (C) 2020-2023 Federico Peinado
   http://www.federicopeinado.com
   Este fichero forma parte del material de la asignatura Inteligencia Artificial para Videojuegos.
   Esta asignatura se imparte en la Facultad de Informática de la Universidad Complutense de Madrid (España).
   Autor: Federico Peinado 
   Contacto: email@federicopeinado.com
*/
namespace UCM.IAV.Navegacion
{
    using UnityEngine;
    using System;

    // Vértice, también llamado registro de nodo, o registro de punto representativo (común a la mayoría de esquemas de división)
    // En el pseudocódigo de Millington esta clase se llama NodeRecord, en vez de Vertex y además de guardar el id del nodo y el coste f, se guarda el id del padre de donde venimos y también el coste g
    [System.Serializable]
    public class Vertex : MonoBehaviour, IComparable<Vertex>, IEquatable<Vertex>
    {
        /// <summary>
        /// Identificador del vértice 
        /// </summary>
        public int id;

        /// <summary>
        /// Coste total estimado (f) del vértice 
        /// </summary>
        public float cost;

        // Para comparar vértices, comparo el coste lo primero, que es lo principal... si el coste es igual, ver de que IDs de nodos estoy hablando
        // (hay que procurar cumplir la convención de que si dos objetos son equals, el CompareTo DEBE devolver un cero)
        public int CompareTo(Vertex other)
        {
            // Código antiguo para borrar
            //float result = this.cost - other.cost;
            //return (int)(Mathf.Sign(result) * Mathf.Ceil(Mathf.Abs(result)));

            // 1. Control de nulos
            if (other == null) return 1;

            // 2. Consistencia con Equals: Si tienen el mismo ID, son "el mismo" registro
            // Esto es vital para que las colecciones de .NET no se rompan
            if (this.id == other.id) return 0;

            //if (ReferenceEquals(this, other)) return 0;

            // 1. Comparar por coste (Prioridad principal para ordenar vértices)
            int costComparison = this.cost.CompareTo(other.cost);
            if (costComparison != 0)
            {
                return costComparison;
            }
             
            // 4. Desempate: Si tienen distinto ID pero el mismo coste, NO debemos devolver 0
            // porque si devolvemos 0, el Equals no tendría consistencia y la colección pensaría que son el mismo nodo.
            if (costComparison == 0)
            {
                return this.id.CompareTo(other.id);
            }

            return costComparison;
        }

        // Implementación de IEquatable<Vertex> (más eficiente que el Equals genérico)
        // Si dos vertices tienen el mismo ID, son 'el mismo registro' (se asume que no puede haber varios registros DE UN MISMO NODO en las listas...)
        public bool Equals(Vertex other)
        {
            // Código antiguo para borrar
            //return (other.id == this.id);

            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            //Vertex other = (Vertex)obj;
            //if (ReferenceEquals(obj, null)) return false;
            //return (other.id == this.id);
            return Equals(obj as Vertex);
        }

        // El HashCode debe basarse en lo mismo que Equals (el id)
        public override int GetHashCode()
        {
            return this.id.GetHashCode();
        }

        // Recomendado: Sobrecargar operadores si usas comparaciones directas (==)
        public static bool operator ==(Vertex left, Vertex right)
        {
            if (ReferenceEquals(left, null)) return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(Vertex left, Vertex right)
        {
            return !(left == right);
        }

    }
}