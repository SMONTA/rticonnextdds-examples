/*******************************************************************************
 (c) 2005-2014 Copyright, Real-Time Innovations, Inc.  All rights reserved.
 RTI grants Licensee a license to use, modify, compile, and create derivative
 works of the Software.  Licensee has the right to distribute object form only
 for use with RTI products.  The Software is provided "as is", with no warranty
 of any type, including any warranty for fitness for any purpose. RTI is under
 no obligation to maintain or support the Software.  RTI shall not be liable for
 any incidental or consequential damages arising out of the use or inability to
 use the software.
 ******************************************************************************/

/*   
ccf_publisher.cs

A publication of data of type ccf

This file is derived from code automatically generated by the rtiddsgen 
command:

rtiddsgen -language C# -example <arch> ccf.idl

Example publication of type ccf automatically generated by 
'rtiddsgen'. To test them follow these steps:

(1) Compile this file and the example subscription.

(2) Start the subscription with the command
objs\<arch>${constructMap.nativeFQNameInModule}_subscriber <domain_id> <sample_count>

(3) Start the publication with the command
objs\<arch>${constructMap.nativeFQNameInModule}_publisher <domain_id> <sample_count>

(4) [Optional] Specify the list of discovery initial peers and 
multicast receive addresses via an environment variable or a file 
(in the current working directory) called NDDS_DISCOVERY_PEERS. 

You can run any number of publishers and subscribers programs, and can 
add and remove them dynamically from the domain.

Example:

To run the example application on domain <domain_id>:

bin\<Debug|Release>\ccf_publisher <domain_id> <sample_count>
bin\<Debug|Release>\ccf_subscriber <domain_id> <sample_count>
*/

using System;
using System.Collections.Generic;
using System.Text;

public class ccfPublisher {

    public static void Main(string[] args) {

        // --- Get domain ID --- //
        int domain_id = 0;
        if (args.Length >= 1) {
            domain_id = Int32.Parse(args[0]);
        }

        // --- Get max loop count; 0 means infinite loop  --- //
        int sample_count = 0;
        if (args.Length >= 2) {
            sample_count = Int32.Parse(args[1]);
        }

        /* Uncomment this to turn on additional logging
        NDDS.ConfigLogger.get_instance().set_verbosity_by_category(
            NDDS.LogCategory.NDDS_CONFIG_LOG_CATEGORY_API, 
            NDDS.LogVerbosity.NDDS_CONFIG_LOG_VERBOSITY_STATUS_ALL);
        */

        // --- Run --- //
        try {
            ccfPublisher.publish(
                domain_id, sample_count);
        }
        catch(DDS.Exception)
        {
            Console.WriteLine("error in publisher");
        }
    }

    static void publish(int domain_id, int sample_count) {

        // --- Create participant --- //

        /* To customize participant QoS, use 
        the configuration file USER_QOS_PROFILES.xml */
        DDS.DomainParticipant participant =
        DDS.DomainParticipantFactory.get_instance().create_participant(
            domain_id,
            DDS.DomainParticipantFactory.PARTICIPANT_QOS_DEFAULT, 
            null /* listener */,
            DDS.StatusMask.STATUS_MASK_NONE);
        if (participant == null) {
            shutdown(participant);
            throw new ApplicationException("create_participant error");
        }

        // --- Create publisher --- //

        /* To customize publisher QoS, use 
        the configuration file USER_QOS_PROFILES.xml */
        DDS.Publisher publisher = participant.create_publisher(
            DDS.DomainParticipant.PUBLISHER_QOS_DEFAULT,
            null /* listener */,
            DDS.StatusMask.STATUS_MASK_NONE);
        if (publisher == null) {
            shutdown(participant);
            throw new ApplicationException("create_publisher error");
        }

        // --- Create topic --- //

        /* Register type before creating topic */
        System.String type_name = ccfTypeSupport.get_type_name();
        try {
            ccfTypeSupport.register_type(
                participant, type_name);
        }
        catch(DDS.Exception e) {
            Console.WriteLine("register_type error {0}", e);
            shutdown(participant);
            throw e;
        }

        /* To customize topic QoS, use 
        the configuration file USER_QOS_PROFILES.xml */
        DDS.Topic topic = participant.create_topic(
            "Example ccf",
            type_name,
            DDS.DomainParticipant.TOPIC_QOS_DEFAULT,
            null /* listener */,
            DDS.StatusMask.STATUS_MASK_NONE);
        if (topic == null) {
            shutdown(participant);
            throw new ApplicationException("create_topic error");
        }

        /* Start changes for Custom_Content_Filter */
        /* Create and register custom filter */
        custom_filter_type custom_filter = new custom_filter_type();
        try {
            participant.register_contentfilter("Customfilter", custom_filter);
        }
        catch (DDS.Exception e) {
            Console.WriteLine("write error {0}", e);
        }
        /* End changes for Custom_Content_Filter */

        // --- Create writer --- //

        /* To customize data writer QoS, use 
        the configuration file USER_QOS_PROFILES.xml */
        DDS.DataWriter writer = publisher.create_datawriter(
            topic,
            DDS.Publisher.DATAWRITER_QOS_DEFAULT,
            null /* listener */,
            DDS.StatusMask.STATUS_MASK_NONE);
        if (writer == null) {
            shutdown(participant);
            throw new ApplicationException("create_datawriter error");
        }
        ccfDataWriter ccf_writer =
        (ccfDataWriter)writer;

        // --- Write --- //

        /* Create data sample for writing */
        ccf instance = ccfTypeSupport.create_data();
        if (instance == null) {
            shutdown(participant);
            throw new ApplicationException(
                "ccfTypeSupport.create_data error");
        }

        /* For a data type that has a key, if the same instance is going to be
        written multiple times, initialize the key here
        and register the keyed instance prior to writing */
        DDS.InstanceHandle_t instance_handle = DDS.InstanceHandle_t.HANDLE_NIL;
        /*
        instance_handle = ccf_writer.register_instance(instance);
        */

        /* Main loop */
        const System.Int32 send_period = 1000; // milliseconds
        for (int count=0;
        (sample_count == 0) || (count < sample_count);
        ++count) {
            Console.WriteLine("Writing ccf, count {0}", count);

            /* Modify the data to be sent here */
            instance.x = count;

            try {
                ccf_writer.write(instance, ref instance_handle);
            }
            catch(DDS.Exception e) {
                Console.WriteLine("write error {0}", e);
            }

            System.Threading.Thread.Sleep(send_period);
        }

        /*
        try {
            ccf_writer.unregister_instance(
                instance, ref instance_handle);
        } catch(DDS.Exception e) {
            Console.WriteLine("unregister instance error: {0}", e);
        }
        */

        // --- Shutdown --- //

        /* Delete data sample */
        try {
            ccfTypeSupport.delete_data(instance);
        } catch(DDS.Exception e) {
            Console.WriteLine(
                "ccfTypeSupport.delete_data error: {0}", e);
        }

        /* Delete all entities */
        shutdown(participant);
    }

    static void shutdown(
        DDS.DomainParticipant participant) {

        /* Delete all entities */

        if (participant != null) {
            participant.delete_contained_entities();
            DDS.DomainParticipantFactory.get_instance().delete_participant(
                ref participant);
        }

        /* RTI Connext provides finalize_instance() method on
        domain participant factory for people who want to release memory
        used by the participant factory. Uncomment the following block of
        code for clean destruction of the singleton. */
        /*
        try {
            DDS.DomainParticipantFactory.finalize_instance();
        } catch (DDS.Exception e) {
            Console.WriteLine("finalize_instance error: {0}", e);
            throw e;
        }
        */
    }
}


/* custom_filter_type class
 *
 * This class contains the functions needed by the Custom Content Filter to work.
 *
 * See the example README.txt file for details about each of these functions.
 *
 *  modification history
 *  ------------ -------
 *  2Mar2015,amb Example adapted for RTI Connext DDS 5.2
 */

public class custom_filter_type : DDS.IContentFilter {

    private interface evaluate_function {
        bool eval(long sample_data);
    }

    private class divide_test : evaluate_function {
        long _p = 1;

        public divide_test(long p) {
            _p = p;
        }

        public bool eval(long sample_data) {
            return (sample_data % _p == 0);
        }
    }

    private class gt_test : evaluate_function {
        long _p = 1;

        public gt_test(long p) {
            _p = p;
        }

        public bool eval(long sample_data) {
            return (sample_data > _p);
        }
    }

    /* Called when Custom Filter is created, or when parameters are changed */
    public void compile(ref object compile_data, string expression,
        DDS.StringSeq parameters, DDS.TypeCode type_code,
        string type_class_name, object old_compile_data) {

        /* We expect an expression of the form "%0 %1 <var>"
         * where %1 = "divides" or "greater-than"
         * and <var> is an integral member of the msg structure.
         * 
         * We don't actually check that <var> has the correct typecode,
         * (or even that it exists!). See example Typecodes to see 
         * how to work with typecodes.
         *
         * The compile information is a structure including the first filter
         * parameter (%0) and a function pointer to evaluate the sample
         */

        if (expression.StartsWith("%0 %1 ") && expression.Length > 6
            && parameters.length > 1) { // Enought parameters?
            long p = Convert.ToInt64(parameters.get_at(0));

            if (String.Compare(parameters.get_at(1), "greater-than") == 0) {
                compile_data = new gt_test(p);
                return;
            }
            else if (String.Compare(parameters.get_at(1), "divides") == 0) {
                compile_data = new divide_test(p);
                return;
            }
        }

        Console.WriteLine("CustomFilter: Unable to compile expresssion '"
            + expression + "'");
        Console.WriteLine("              with parameters '" + parameters.get_at(0)
            + "' '" + parameters.get_at(1) + "'");
        //throw (new DDS.Retcode_BadParameter());
    }

    /* Called to evaluated each sample */
    public bool evaluate(object compile_data, object sample, ref
        DDS.FilterSampleInfo meta_data) {
        long x = ((ccf)sample).x;
        return ((evaluate_function)compile_data).eval(x);
    }

    public void finalize(object compile_data) {
    }
}