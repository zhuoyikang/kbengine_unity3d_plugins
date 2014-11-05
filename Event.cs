﻿namespace KBEngine
{
    using UnityEngine;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    public class Event
    {
        public struct Pair
        {
            public object obj;
            public string funcname;
            public System.Reflection.MethodInfo method;
        };

        public struct EventObj
        {
            public Pair info;
            public object[] args;
        };

    	public static Dictionary<string, List<Pair>> events_out = new Dictionary<string, List<Pair>>();

        public static LinkedList<EventObj> firedEvents_out = new LinkedList<EventObj>();
        private static LinkedList<EventObj> doingEvents_out = new LinkedList<EventObj>();

    	public static Dictionary<string, List<Pair>> events_in = new Dictionary<string, List<Pair>>();

        public static LinkedList<EventObj> firedEvents_in = new LinkedList<EventObj>();
        private static LinkedList<EventObj> doingEvents_in = new LinkedList<EventObj>();

        private static bool _isPauseOut = false;

        public Event()
        {
        }

        public static void clear()
        {
            events_out.Clear();
            events_in.Clear();
            clearFiredEvents();
        }

        public static void clearFiredEvents()
        {
            firedEvents_out.Clear();
            doingEvents_out.Clear();
            firedEvents_in.Clear();
            doingEvents_in.Clear();

            _isPauseOut = false;
        }

        public static void pause()
        {
            _isPauseOut = true;
        }

        public static void resume()
        {
            _isPauseOut = false;
        }

        public static bool isPause()
        {
            return _isPauseOut;
        }

        public static bool hasRegisterOut(string eventname)
        {
            return _hasRegister(events_out, eventname);
        }

        public static bool hasRegisterIn(string eventname)
        {
            return _hasRegister(events_in, eventname);
        }

        private static bool _hasRegister(Dictionary<string, List<Pair>> events, string eventname)
        {
            bool has = false;

            Monitor.Enter(events);
            has = events.ContainsKey(eventname);
            Monitor.Exit(events);

            return has;
        }

        /*
          注册监听由kbe插件抛出的事件。(out = kbe->render)
          通常由渲染表现层来注册, 例如：监听角色血量属性的变化， 如果UI层注册这个事件，
          事件触发后就可以根据事件所附带的当前血量值来改变角色头顶的血条值。
        */
        public static bool registerOut(string eventname, object obj, string funcname)
        {
            return register(events_out, eventname, obj, funcname);
        }

        /*
          注册监听由渲染表现层抛出的事件(in = render->kbe)
          通常由kbe插件层来注册， 例如：UI层点击登录， 此时需要触发一个事件给kbe插件层进行与服务端交互的处理。
        */
        public static bool registerIn(string eventname, object obj, string funcname)
        {
            return register(events_in, eventname, obj, funcname);
        }

        private static bool register(Dictionary<string, List<Pair>> events, string eventname, object obj, string funcname)
        {
            deregister(events, eventname, obj, funcname);
            List<Pair> lst = null;

            Pair pair = new Pair();
            pair.obj = obj;
            pair.funcname = funcname;
            pair.method = obj.GetType().GetMethod(funcname);
            if(pair.method == null)
            {
                Dbg.ERROR_MSG("Event::register: " + obj + "not found method[" + funcname + "]");
                return false;
            }

            Monitor.Enter(events);
            if(!events.TryGetValue(eventname, out lst))
            {
                lst = new List<Pair>();
                lst.Add(pair);
                Dbg.DEBUG_MSG("Event::register: event(" + eventname + ")!");
                events.Add(eventname, lst);
                Monitor.Exit(events);
                return true;
            }

            Dbg.DEBUG_MSG("Event::register: event(" + eventname + ")!");
            lst.Add(pair);
            Monitor.Exit(events);
            return true;
        }

        public static bool deregisterOut(string eventname, object obj, string funcname)
        {
            return deregister(events_out, eventname, obj, funcname);
        }

        public static bool deregisterIn(string eventname, object obj, string funcname)
        {
            return deregister(events_in, eventname, obj, funcname);
        }

        private static bool deregister(Dictionary<string, List<Pair>> events, string eventname, object obj, string funcname)
        {
            Monitor.Enter(events);
            List<Pair> lst = null;

            if(!events.TryGetValue(eventname, out lst))
            {
                Monitor.Exit(events);
                return false;
            }

            for(int i=0; i<lst.Count; i++)
            {
                if(obj == lst[i].obj && lst[i].funcname == funcname)
                {
                    Dbg.DEBUG_MSG("Event::deregister: event(" + eventname + ":" + funcname + ")!");
                    lst.RemoveAt(i);
                    Monitor.Exit(events);
                    return true;
                }
            }

            Monitor.Exit(events);
            return false;
        }

        public static bool deregisterOut(object obj)
        {
            return deregister(events_out, obj);
        }

        public static bool deregisterIn(object obj)
        {
            return deregister(events_in, obj);
        }

        private static bool deregister(Dictionary<string, List<Pair>> events, object obj)
        {
            Monitor.Enter(events);

            foreach(KeyValuePair<string, List<Pair>> e in events)
            {
                List<Pair> lst = e.Value;
                __RESTART_REMOVE:
                for(int i=0; i<lst.Count; i++)
                {
                    if(obj == lst[i].obj)
                    {
                        Dbg.DEBUG_MSG("Event::deregister: event(" + e.Key + ":" + lst[i].funcname + ")!");
                        lst.RemoveAt(i);
                        goto __RESTART_REMOVE;
                    }
                }
            }

            Monitor.Exit(events);
            return true;
        }

        /*
          kbe插件触发事件(out = kbe->render)
          通常由渲染表现层来注册, 例如：监听角色血量属性的变化， 如果UI层注册这个事件，
          事件触发后就可以根据事件所附带的当前血量值来改变角色头顶的血条值。
        */
        public static void fireOut(string eventname, object[] args)
        {
            fire_(events_out, firedEvents_out, eventname, args);
        }

        /*
          渲染表现层抛出事件(in = render->kbe)
          通常由kbe插件层来注册， 例如：UI层点击登录， 此时需要触发一个事件给kbe插件层进行与服务端交互的处理。
        */
        public static void fireIn(string eventname, object[] args)
        {
            fire_(events_in, firedEvents_in, eventname, args);
        }

        /*
          触发kbe插件和渲染表现层都能够收到的事件
        */
        public static void fireAll(string eventname, object[] args)
        {
            fire_(events_in, firedEvents_in, eventname, args);
            fire_(events_out, firedEvents_out, eventname, args);
        }

        private static void fire_(Dictionary<string, List<Pair>> events, LinkedList<EventObj> firedEvents, string eventname, object[] args)
        {
            Monitor.Enter(events);
            List<Pair> lst = null;

            if(!events.TryGetValue(eventname, out lst))
            {
                if(events == events_in)
                    Dbg.WARNING_MSG("Event::fireIn: event(" + eventname + ") not found!");
                else
                    Dbg.WARNING_MSG("Event::fireOut: event(" + eventname + ") not found!");

                Monitor.Exit(events);
                return;
            }

            for(int i=0; i<lst.Count; i++)
            {
                EventObj eobj = new EventObj();
                eobj.info = lst[i];
                eobj.args = args;
                firedEvents.AddLast(eobj);
            }

            Monitor.Exit(events);
        }

        public static void processOutEvents()
        {
            Monitor.Enter(events_out);

            if(firedEvents_out.Count > 0)
            {
                foreach(EventObj evt in firedEvents_out)
                {
                    doingEvents_out.AddLast(evt);
                }

                firedEvents_out.Clear();
            }

            Monitor.Exit(events_out);

            while (doingEvents_out.Count > 0 && !_isPauseOut)
            {

                EventObj eobj = doingEvents_out.First.Value;

                //Debug.Log("processOutEvents:" + eobj.info.funcname + "(" + eobj.info + ")");
                //foreach(object v in eobj.args)
                //{
                //	Debug.Log("processOutEvents:args=" + v);
                //}
                try
                {
                    eobj.info.method.Invoke (eobj.info.obj, eobj.args);
                }
                catch (Exception e)
                {
                    Dbg.ERROR_MSG(e.ToString());
                    Dbg.ERROR_MSG("Event::processOutEvents: event=" + eobj.info.funcname);
                }

                doingEvents_out.RemoveFirst();
            }
        }

        public static void processInEvents()
        {
            Monitor.Enter(events_in);

            if(firedEvents_in.Count > 0)
            {
                foreach(EventObj evt in firedEvents_in)
                {
                    doingEvents_in.AddLast(evt);
                }

                firedEvents_in.Clear();
            }

            Monitor.Exit(events_in);

            while (doingEvents_in.Count > 0)
            {

                EventObj eobj = doingEvents_in.First.Value;

                //Debug.Log("processInEvents:" + eobj.info.funcname + "(" + eobj.info + ")");
                //foreach(object v in eobj.args)
                //{
                //	Debug.Log("processInEvents:args=" + v);
                //}
                try
                {
                    eobj.info.method.Invoke (eobj.info.obj, eobj.args);

                }
                catch (Exception e)
                {
                    Dbg.ERROR_MSG(e.ToString());
                    Dbg.ERROR_MSG("Event::processInEvents: event=" + eobj.info.funcname);
                }

                doingEvents_in.RemoveFirst();
            }
        }

    }
}
