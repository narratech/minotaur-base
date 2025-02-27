# Minotaur - Base
Proyecto de videojuego actualizado a **Unity 2022.3.40f1** diseñador para servir como punto de partida en algunas prácticas.

Consiste en un entorno virtual 3D que representa el Laberinto del Minotauro, generado procedimentalmente, un personaje controlable por el jugador que es Teseo y uno o varios minotauros para ser controlados mediante IA.

## Licencia
Federico Peinado, autor de la documentación, código y recursos de este trabajo, concedo permiso permanente a los alumnos de la Facultad de Informática de la Universidad Complutense de Madrid para utilizar este material, con sus comentarios y evaluaciones, con fines educativos o de investigación; ya sea para obtener datos agregados de forma anónima como para utilizarlo total o parcialmente reconociendo expresamente mi autoría.

## Notas
Sobre esto hay quien implementa el A* con una estructura de registro de nodo muy simple (el identificador del nodo y el coste f), sólo usa lista de apertura, se apoya en tener toda la información completa del grafo a mano (costes incluidos) y como estructura de datos auxiliar usa una cola de prioridad muy simple.
Según el pseudocódigo que plantea Millington, la estructura de registro de nodo es más rica (identificador del nodo, conexión con el nodo padre, coste g y coste f), se usa una lista de apertura y una lista de cierre, no se asume que toda la información del grafo esté disponible y la cola de prioridad se puede implementar con PriorityQueue<TElement, TPriority> (estructura que se encuentra en el espacio de nombres System.Collections.Generic y fue introducida en .NET 6) o con un BinaryHeap como este: https://github.com/NikolajLeischner/csharp-binary-heap.

## Referencias
Los recursos de terceros utilizados son de uso público.
* *AI for Games*, Ian Millington.
* [Kaykit Medieval Builder Pack](https://kaylousberg.itch.io/kaykit-medieval-builder-pack)
* [Kaykit Dungeon](https://kaylousberg.itch.io/kaykit-dungeon)
* [Kaykit Animations](https://kaylousberg.itch.io/kaykit-animations)
