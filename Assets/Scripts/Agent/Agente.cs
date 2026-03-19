/*    
   Copyright (C) 2020-2023 Federico Peinado
   http://www.federicopeinado.com
   Este fichero forma parte del material de la asignatura Inteligencia Artificial para Videojuegos.
   Esta asignatura se imparte en la Facultad de Inform�tica de la Universidad Complutense de Madrid (Espa�a).
   Autor: Federico Peinado 
   Contacto: email@federicopeinado.com
*/
namespace UCM.IAV.Movimiento {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine; 

/// <summary>
/// La clase Agente es responsable de modelar los agentes y gestionar todos los comportamientos asociados para combinarlos (si es posible) 
/// </summary>
    public class Agente : MonoBehaviour {
        /// <summary>
        /// Combinar por peso
        /// </summary>
        [Tooltip("Combinar por peso")]
        public bool combinarPorPeso = false;

        /// <summary>
        /// Combinar por prioridad
        /// </summary>
        [Tooltip("Combinar por prioridad")]
        public bool combinarPorPrioridad = false;

        /// <summary>
        /// Umbral de prioridad para tener el valor en cuenta
        /// </summary>
        [Tooltip("Umbral de prioridad")]
        public float umbralPrioridad = 0.2f;

        /// <summary>
        /// Velocidad m�xima
        /// </summary>
        [Tooltip("Velocidad (lineal) m�xima")]
        public float velocidadMax;

        /// <summary>
        /// Rotaci�n m�xima
        /// </summary>
        [Tooltip("Rotaci�n (velocidad angular) m�xima")]
        public float rotacionMax;

        /// <summary>
        /// Aceleraci�n m�xima
        /// </summary>
        [Tooltip("Aceleraci�n (lineal) m�xima")]
        public float aceleracionMax;

        /// <summary>
        /// Aceleraci�n angular m�xima
        /// </summary>
        [Tooltip("Aceleraci�n angular m�xima")]
        public float aceleracionAngularMax;

        /// <summary>
        /// Velocidad (se puede dar una velocidad de inicio).
        /// </summary>
        [Tooltip("Velocidad")]
        public Vector3 velocidad;

        /// <summary>
        /// Rotaci�n (o velocidad angular; se puede dar una rotaci�n de inicio)
        /// </summary>
        [Tooltip("Rotaci�n (velocidad angular)")]
        public float rotacion;

        /// <summary>
        /// Orientacion (hacia donde encara el agente)
        /// </summary>
        [Tooltip("Orientaci�n")]
        public float orientacion;

        /// <summary>
        /// Valor de direcci�n (instrucciones de movimiento)
        /// </summary>
        [Tooltip("Direcci�n (instrucciones de movimiento)")]
        protected Direccion direccion;

        /// <summary>
        /// Grupos de direcciones, organizados seg�n su prioridad
        /// </summary>
        [Tooltip("Grupos de direcciones")]
        private Dictionary<int, List<Direccion>> grupos;

        /// <summary>
        /// Componente de cuerpo r�gido (si la tiene el agente)
        /// </summary>
        [Tooltip("Cuerpo r�gido")]
        private Rigidbody cuerpoRigido;

        /// <summary>
        /// Constante del tiempo de giro
        /// </summary>
        [Tooltip("Tiempo de giro (al cambiar de direccion)")]
        private float tiempoGiroSuave = 0.1f;

        /// <summary>
        /// Variable de referencia para damping
        /// </summary>
        [Tooltip("Referencia para el giro")]
        float velocidadGiroSuave;

        /// <summary>
        /// Al comienzo, se inicialian algunas variables
        /// </summary>
        void Start()
        {
            // Descomentar estas l�neas si queremos ignorar los valores iniciales de velocidad y rotaci�n
            //velocidad = Vector3.zero; 
            //rotacion = 0.0f
            direccion = new Direccion();
            grupos = new Dictionary<int, List<Direccion>>();

            cuerpoRigido = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// En cada tick fijo, si hay movimiento din�mico, uso el simulador f�sico aplicando las fuerzas que corresponda para moverlo.
        /// Un cuerpo r�gido se puede mover con movePosition, cambiando la velocity o aplicando fuerzas, que es lo habitual y que permite respetar otras fuerzas que est�n actuando sobre �l a la vez.
        /// </summary>
        public virtual void FixedUpdate()
        {
            if (cuerpoRigido.isKinematic)
                return; // El movimiento ser� cinem�tico, fotograma a fotograma con Update

            // Limitamos la aceleraci�n al m�ximo que acepta este agente (aunque normalmente vendr� ya limitada); evitamos hacer raices cuadradas
            if (direccion.lineal.sqrMagnitude > aceleracionMax * aceleracionMax)
                direccion.lineal = direccion.lineal.normalized * aceleracionMax; 

            // La opci�n por defecto ser�a usar ForceMode.Force, pero eso implicar�a que el comportamiento de direcci�n tuviese en cuenta la masa a la hora de calcular la aceleraci�n que se pide
            cuerpoRigido.AddForce(direccion.lineal, ForceMode.Acceleration);

            // Limitamos la aceleraci�n angular al m�ximo que acepta este agente (aunque normalmente vendr� ya limitada)
            if (direccion.angular > aceleracionAngularMax)
                direccion.angular = aceleracionAngularMax;

            // Rotamos el objeto siempre sobre su eje Y (hacia arriba), asumiendo que el agente est� sobre un plano y quiere mirar a un lado o a otro
            // La opci�n por defecto ser�a usar ForceMode.Force, pero eso implicar�a que el comportamiento de direcci�n tuviese en cuenta la masa a la hora de calcular la aceleraci�n que se pide
            cuerpoRigido.AddTorque(transform.up * direccion.angular, ForceMode.Acceleration);

            /* El tema de la orientaci�n, descomentarlo si queremos sobreescribir toda la cuesti�n de la velocidad angular
            orientacion += rotacion / Time.deltaTime; // En lugar de * he puesto / para as� calcular la aceleraci�n, que es lo que debe ir aqu�
            // Necesitamos "constre�ir" inteligentemente la orientaci�n al rango (0, 360)
            if (orientacion < 0.0f)
                orientacion += 360.0f;
            else if (orientacion > 360.0f)
                orientacion -= 360.0f;

            Vector3 orientationVector = OriToVec(orientacion);
            cuerpoRigido.rotation = Quaternion.LookRotation(orientationVector, Vector3.up);
            */
            LookDirection();

            // Aunque tambi�n se controlen los m�ximos en el LateUpdate, entiendo que conviene tambi�n hacerlo aqu�, en FixedUpdate, que puede llegar a ejecutarse m�s veces

            // Limito la velocidad lineal al terminar 
            if (cuerpoRigido.linearVelocity.magnitude > velocidadMax)
                cuerpoRigido.linearVelocity = cuerpoRigido.linearVelocity.normalized * velocidadMax;

            // Limito la velocidad angular al terminar
            if (cuerpoRigido.angularVelocity.magnitude > rotacionMax)
                cuerpoRigido.angularVelocity = cuerpoRigido.angularVelocity.normalized * rotacionMax;
            if (cuerpoRigido.angularVelocity.magnitude < -rotacionMax)
                cuerpoRigido.angularVelocity = cuerpoRigido.angularVelocity.normalized * -rotacionMax;
        }

        /// <summary>
        /// En cada tick, hace lo b�sico del movimiento cinem�tico del agente
        /// Un objeto que no atiende a f�sicas se mueve a base de trasladar su transformada.
        /// Al no haber Freeze Rotation, ni rozamiento ni nada... seguramente vaya todo mucho m�s r�pido en cinem�tico que en din�mico
        /// </summary>
        public virtual void Update()
        {
            if (!cuerpoRigido.isKinematic)
                return; // El movimiento ser� din�mico, controlado por la f�sica y FixedUpdate

            // Limito la velocidad lineal antes de empezar; evitamos hacer raices cuadradas
            if (velocidad.sqrMagnitude > velocidadMax * velocidadMax)
                velocidad= velocidad.normalized * velocidadMax;

            // Limito la velocidad angular antes de empezar
            if (rotacion > rotacionMax)
                rotacion = rotacionMax;
            if (rotacion < -rotacionMax)
                rotacion = -rotacionMax;

            Vector3 desplazamiento = velocidad * Time.deltaTime;
            transform.Translate(desplazamiento, Space.World);

            orientacion += rotacion * Time.deltaTime;
            // Vamos a mantener la orientaci�n siempre en el rango can�nico de 0 a 360 grados
            if (orientacion < 0.0f)
                orientacion += 360.0f;
            else if (orientacion > 360.0f)
                orientacion -= 360.0f;

            LookDirection();

            // Elimino la rotaci�n totalmente, dej�ndolo en el estado inicial, antes de rotar el objeto lo que nos marque la variable orientaci�n
            transform.rotation = new Quaternion();
            transform.Rotate(Vector3.up, orientacion);

        }

        /// <summary>
        /// En cada parte tard�a del tick, hace tareas de correcci�n num�rica 
        /// </summary>
        public virtual void LateUpdate()
        {
            if (combinarPorPrioridad)
            {
                direccion = GetPrioridadDireccion();
                grupos.Clear();
            }

            if (cuerpoRigido != null) {
                return; // El movimiento ser� din�mico, controlado por la f�sica y FixedUpdate
            }

            // Limitamos la aceleraci�n al m�ximo que acepta este agente (aunque normalmente vendr� ya limitada); evitamos hacer raices cuadradas
            if (direccion.lineal.sqrMagnitude > aceleracionMax * aceleracionMax)
                direccion.lineal = direccion.lineal.normalized * aceleracionMax; 

            // Limitamos la aceleraci�n angular al m�ximo que acepta este agente (aunque normalmente vendr� ya limitada)
            if (direccion.angular > aceleracionAngularMax)
                direccion.angular = aceleracionAngularMax;

            // Aqu� se calcula la pr�xima velocidad y rotaci�n en funci�n de las aceleraciones  
            velocidad += direccion.lineal * Time.deltaTime;
            rotacion += direccion.angular * Time.deltaTime;

            // Opcional: Esto es para actuar con contundencia si nos mandan parar (no es muy realista)
            if (direccion.angular == 0.0f) 
                rotacion = 0.0f; 
            if (direccion.lineal.sqrMagnitude == 0.0f) 
                velocidad = Vector3.zero; 

            /// En cada parte tard�a del tick, encarar el agente (al menos para el avatar).... si es que queremos hacer este encaramiento
            transform.LookAt(transform.position + velocidad);

            // Se deja la direcci�n vac�a para el pr�ximo fotograma
            direccion = new Direccion();
        }


        /// <summary>
        /// Establece la direcci�n tal cual
        /// </summary>
        public void SetDireccion(Direccion direccion)
        {
            this.direccion = direccion;
        }

        /// <summary>
        /// Establece la direcci�n por peso
        /// </summary>
        /// <param name="direccion"></param>
        /// <param name="peso"></param>
        public void SetDireccion(Direccion direccion, float peso)
        {
            this.direccion.lineal += (peso * direccion.lineal);
            this.direccion.angular += (peso * direccion.angular);
        }

        /// <summary>
        /// Establece la direcci�n por prioridad
        /// </summary>
        /// <param name="direccion"></param>
        /// <param name="prioridad"></param>
        public void SetDireccion(Direccion direccion, int prioridad)
        {
            if (!grupos.ContainsKey(prioridad))
            {
                grupos.Add(prioridad, new List<Direccion>());
            }
            grupos[prioridad].Add(direccion);
        }

        /// <summary>
        /// Devuelve el valor de direcci�n calculado por prioridad
        /// </summary>
        /// <returns></returns>
        private Direccion GetPrioridadDireccion()
        {
            Direccion direccion = new Direccion();
            List<int> gIdList = new List<int>(grupos.Keys);
            gIdList.Sort();
            foreach (int gid in gIdList)
            {
                direccion = new Direccion();
                foreach (Direccion direccionIndividual in grupos[gid])
                {
                    // Dentro del grupo la mezcla es por peso
                    direccion.lineal += direccionIndividual.lineal;
                    direccion.angular += direccionIndividual.angular;
                }
                // S�lo si el resultado supera un umbral, entonces nos quedamos con esta salida y dejamos de mirar grupos con menos prioridad
                if (direccion.lineal.magnitude > umbralPrioridad
                     || Mathf.Abs(direccion.angular) > umbralPrioridad)
                {
                    return direccion;
                }
            }
            return direccion;
        }

        /// <summary>
        /// Calculates el Vector3 dado un cierto valor de orientaci�n
        /// </summary>
        /// <param name="orientacion"></param>
        /// <returns></returns>
        public Vector3 OriToVec(float orientacion)
        {
            Vector3 vector = Vector3.zero;
            vector.x = Mathf.Sin(orientacion * Mathf.Deg2Rad) * 1.0f; //  * 1.0f se a�ade para asegurar que el tipo es float
            vector.z = Mathf.Cos(orientacion * Mathf.Deg2Rad) * 1.0f; //  * 1.0f se a�ade para asegurar que el tipo es float
            return vector.normalized;
        }

        private void LookDirection()
        {
            if (direccion.lineal.x != 0 || direccion.lineal.z != 0)
            {
                //Rotaci�n del personaje hacia donde camina (suavizado)
                float anguloDestino = Mathf.Atan2(direccion.lineal.x, direccion.lineal.z) * Mathf.Rad2Deg;
                //Esto es raro pero Brackeys dice que funciona
                float anguloSuave = Mathf.SmoothDampAngle(transform.eulerAngles.y, anguloDestino, ref velocidadGiroSuave, tiempoGiroSuave);

                transform.rotation = Quaternion.Euler(0f, anguloSuave, 0f);
            }
        }
    }
}
