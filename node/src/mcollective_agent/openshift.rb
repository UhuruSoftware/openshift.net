require 'json'
require 'open3'
require 'logger'

module MCollective
  module Agent
    class Openshift < RPC::Agent

      def run_command(command ,args)
        script = File.join(File.expand_path('../../powershell/oo-cmdlets', __FILE__), "#{command.to_s.gsub('_', '-')}.ps1")
        ps_args = args.to_json.gsub('"', '"""')
        cmd = "powershell.exe -ExecutionPolicy Bypass -InputFormat None -noninteractive -file #{script} \"#{ps_args}\" 2>&1"
        output = ""
        exitcode = 0
        Open3.popen3(cmd) do |stdin, stdout, stderr, wait_thr|
          output = stdout.read
          exitcode = wait_thr.value.exitstatus

          if exitcode == 0
            @logger.debug "EXECUTED COMMAND: #{cmd}"
            @logger.debug "OUT #{command}: EXITCODE: #{exitcode} STDOUT: #{output}"
          else
            @logger.error "EXECUTED COMMAND: #{cmd}"
            @logger.error "OUT #{command}: EXITCODE: #{exitcode} STDOUT: #{output}"
          end
        end
        return exitcode, output
      end

      def echo_action
        @logger.debug "echo_action"
        validate :msg, String
        reply[:msg] = request[:msg]
      end

      def get_facts_action
        @logger.debug "get_facts_action"
      end

      # Handles all incoming messages. Validates the input, executes the action, and constructs
      # a reply.
      def cartridge_do_action
        @logger.debug "cartridge_do_action"
        validate :cartridge, :shellsafe
        validate :action, :shellsafe
        cartridge                  = request[:cartridge]
        action                     = request[:action]
        args                       = request[:args] ||= {}
        pid, stdin, stdout, stderr = nil, nil, nil, nil
        rc                         = nil
        output                     = ""

        @logger.debug "cartridge_do_action: action: '#{action}', cartridge: '#{cartridge}', args: '#{JSON.pretty_generate(args)}'"

        # Do the action execution
        exitcode, output, addtl_params           = execute_action(action, args)

        @logger.debug "!!!EXITCODE IS NIL!!! for action: '#{action}'" if exitcode == nil
        @logger.debug "!!!OUTPUT IS NIL!!! for action: '#{action}'" if output == nil

        reply[:exitcode] = exitcode
        reply[:output]   = output
        reply[:addtl_params] = addtl_params

      end

      # Dispatches the given action to a method on the agent.
      #
      # Returns [exitcode, output] from the resulting action execution.
      def execute_action(action, args)
        action_method = "oo_#{action.gsub('-', '_')}"
        request_id    = args['--with-request-id'].to_s if args['--with-request-id']

        exitcode = 0
        output   = action
        exitcode, output, addtl_params = self.send(action_method.to_sym, args)

        @logger.debug "execute_action - action: #{action}, #{JSON.pretty_generate(args)}"
        return exitcode, output, addtl_params
      end

      # Executes a list of jobs sequentially, adding the exitcode and output
      # from execute_action to each job following the execution.
      #
      # The actual message reply object is set with an exitcode of 0 and
      # output containing the job list (in which the individual execution
      # results are embedded).
      #
      # BZ 876942: Disable threading until we can explore proper concurrency management
      def execute_parallel_action
        @logger.debug "execute_parallel_action - request: #{request}"

        joblist = request[config.identity]

        joblist.each do |parallel_job|
          job = parallel_job[:job]

          cartridge = job[:cartridge]
          action    = job[:action]
          args      = job[:args]

          exitcode, output = execute_action(action, args)

          parallel_job[:result_exit_code] = exitcode
          parallel_job[:result_stdout]    = output
        end

        # Log.instance.info("execute_parallel_action call - #{joblist}")
        @logger.debug "OUT execute_parallel_action - joblist: #{JSON.pretty_generate(joblist)}"
        reply[:output]   = joblist
        reply[:exitcode] = 0
      end

      #
      # Upgrade between versions
      #
      def upgrade_action
        @logger.debug "upgrade_action"
        #TODO we need to implement this
      end

      #
      # Builds a new ApplicationContainer instance from the standard
      # argument payload which is expected for any message used for
      # gear/cart operations.
      #
      # Use this to get a new ApplicationContainer instance in all cases.
      #
      # A new OpenShift::Runtime::Hourglass will be initialized and passed
      # to the ApplicationContainerInstance to allow for timing consistency.
      # The hourglass will be initialized with a duration shorter than the
      # configured MCollective agent timeout.
      #
      def get_app_container_from_args(args)
        @logger.debug "get_app_container_from_args"
      end

      def with_container_from_args(args)
        @logger.debug "with_container_from_args"
        output = ''
      end

      def oo_app_create(args)
        @logger.debug "oo_app_create"
        exitcode, output = run_command(__method__, args)
        @logger.debug output
        return exitcode, output
      end

      def oo_app_destroy(args)
        @logger.debug "oo_app_destroy"
        run_command(__method__, args)
      end

      def oo_activate(args)
        @logger.debug "oo_activate"
        run_command(__method__, args)
      end

      def oo_authorized_ssh_key_add(args)
        @logger.debug "oo_authorized_ssh_key_add"
        run_command(__method__, args)
      end

      def oo_authorized_ssh_key_remove(args)
        @logger.debug "oo_authorized_ssh_key_remove"
        run_command(__method__, args)
      end

      def oo_authorized_ssh_keys_replace(args)
        @logger.debug "oo_authorized_ssh_keys_replace"
        run_command(__method__, args)
      end

      def oo_broker_auth_key_add(args)
        @logger.debug "oo_broker_auth_key_add"
        return run_command(__method__, args)
      end

      def oo_broker_auth_key_remove(args)
        @logger.debug "oo_broker_auth_key_remove"
        return run_command(__method__, args)
      end

      def oo_env_var_add(args)
        @logger.debug "oo_env_var_add"
        return run_command(__method__, args)
      end

      def oo_env_var_remove(args)
        @logger.debug "oo_env_var_remove"
        return run_command(__method__, args)
      end

      def oo_cartridge_list(args)
        @logger.debug "oo_cartridge_list"
        exitcode, output = run_command(__method__, args)
        @logger.debug exitcode
        @logger.debug output
        return exitcode, output
      end

      def oo_app_state_show(args)
        @logger.debug "oo_app_state_show"
        run_command(__method__, args)
      end

      def oo_get_quota(args)
        @logger.debug "oo_get_quota"
      end

      def oo_set_quota(args)
        @logger.debug "oo_set_quota"
      end

      def oo_force_stop(args)
        @logger.debug "oo_force_stop"
        run_command(__method__, args)
      end


      #
      # Instantiate the front-end class from the given arguments and
      # follow proper exception handling pattern.
      #
      def with_frontend_rescue_pattern
        #TODO See if we can remove this
        @logger.debug "with_frontend_rescue_pattern"
        raise "HTTP.SYS can take care of proxying requests"
        output = ""
      end

      def with_frontend_from_args(args)
        @logger.debug "with_frontend_from_args"
        raise "HTTP.SYS can take care of proxying requests"
      end

      def with_frontend_returns_data(args)
        @logger.debug "with_frontend_returns_data"
        raise "HTTP.SYS can take care of proxying requests"
      end

      #
      # A frontend must be created before any other manipulations are
      # performed on it.
      #
      def oo_frontend_create(args)
        @logger.debug "oo_frontend_create"
        raise "HTTP.SYS can take care of proxying requests"
      end

      def oo_frontend_destroy(args)
        @logger.debug "oo_frontend_destroy"
        raise "HTTP.SYS can take care of proxying requests"
      end

      def oo_frontend_update_name(args)
        @logger.debug "oo_frontend_update_name"
        raise "HTTP.SYS can take care of proxying requests"
      end

      #
      # The path-target-option is an array of the path, target and
      # options.  Multiple arrays may be specified and they are in
      # the form of: [ path(String), target(String), options(Hash) ]
      # ex: [ "", "127.0.250.1:8080", { "websocket" => 1 } ], ...
      #
      def oo_frontend_connect(args)
        @logger.debug "oo_frontend_connect"
        raise "HTTP.SYS can take care of proxying requests"
      end

      #
      # The paths are an array of the paths to remove.
      # ex: [ "", "/health", ... ]
      def oo_frontend_disconnect(args)
        @logger.debug "oo_frontend_disconnect"
        raise "HTTP.SYS can take care of proxying requests"
      end

      def oo_frontend_connections(args)
        @logger.debug "oo_frontend_connections"
        raise "HTTP.SYS can take care of proxying requests"
      end

      def oo_frontend_idle(args)
        @logger.debug "oo_frontend_idle"
        raise "HTTP.SYS can take care of proxying requests"
      end

      def oo_frontend_unidle(args)
        @logger.debug "oo_frontend_unidle"
        raise "HTTP.SYS can take care of proxying requests"
      end

      def oo_frontend_check_idle(args)
        @logger.debug "oo_frontend_check_idle"
        raise "HTTP.SYS can take care of proxying requests"
      end

      def oo_frontend_sts(args)
        @logger.debug "oo_frontend_sts"
        raise "HTTP.SYS can take care of proxying requests"
      end

      def oo_frontend_no_sts(args)
        @logger.debug "oo_frontend_no_sts"
        raise "HTTP.SYS can take care of proxying requests"
      end

      def oo_frontend_get_sts(args)
        @logger.debug "oo_frontend_get_sts"
        raise "HTTP.SYS can take care of proxying requests"
      end

      def oo_add_alias(args)
        @logger.debug "oo_add_alias"
        raise "Load Balancer will be deployed on Linux"
      end

      def oo_remove_alias(args)
        @logger.debug "oo_remove_alias"
        raise "Load Balancer will be deployed on Linux"
      end

      def oo_aliases(args)
        @logger.debug "oo_aliases"
        raise "Load Balancer will be deployed on Linux"
      end

      def oo_ssl_cert_add(args)
        @logger.debug "oo_ssl_cert_add"
        raise "Load Balancer will be deployed on Linux"
      end

      def oo_ssl_cert_remove(args)
        @logger.debug "oo_ssl_cert_remove"
        raise "Load Balancer will be deployed on Linux"
      end

      def oo_ssl_certs(args)
        @logger.debug "oo_ssl_certs"
        raise "Load Balancer will be deployed on Linux"
      end

      def oo_frontend_to_hash(args)
        @logger.debug "oo_frontend_to_hash"
        raise "Load Balancer will be deployed on Linux"
      end

      # The backup is just a JSON encoded string
      #
      # TODO: Determine if its necessary to base64 encode
      # the output to protect from interpretation.
      #
      def oo_frontend_backup(args)
        @logger.debug "oo_frontend_backup"
        raise "HTTP.SYS can take care of proxying requests"
      end

      # Does an implicit instantiation of the FrontendHttpServer class.
      def oo_frontend_restore(args)
        @logger.debug "oo_frontend_restore"
        raise "HTTP.SYS can take care of proxying requests"
      end

      def oo_tidy(args)
        @logger.debug "oo_tidy"
        run_command(__method__, args)
      end

      def oo_expose_port(args)
        @logger.debug "oo_expose_port"
        run_command(__method__, args)
      end

      def oo_conceal_port(args)
        @logger.debug "oo_conceal_port"
        run_command(__method__, args)
      end

      def oo_connector_execute(args)
        @logger.debug "oo_connector_execute"
        run_command(__method__, args)
      end

      def oo_configure(args)
        @logger.debug "oo_configure"
        run_command(__method__, args)
      end

      def oo_update_configuration(args)
        @logger.debug "oo_update_configuration"
        run_command(__method__, args)
      end

      def oo_post_configure(args)
        @logger.debug "oo_post_configure"
        run_command(__method__, args)
      end

      def oo_deconfigure(args)
        @logger.debug "oo_deconfigure"
        run_command(__method__, args)
      end

      def oo_unsubscribe(args)
        @logger.debug "oo_unsubscribe"
        run_command(__method__, args)
      end

      def oo_deploy_httpd_proxy(args)
        @logger.debug "oo_deploy_httpd_proxy"
        raise "Load Balancer will be deployed on Linux"
      end

      def oo_remove_httpd_proxy(args)
        @logger.debug "oo_remove_httpd_proxy"
        raise "Load Balancer will be deployed on Linux"
      end

      def oo_restart_httpd_proxy(args)
        @logger.debug "oo_restart_httpd_proxy"
        raise "Load Balancer will be deployed on Linux"
      end

      def oo_system_messages(args)
        @logger.debug "oo_system_messages"
        raise "Load Balancer will be deployed on Linux"      end

      def oo_start(args)
        @logger.debug "oo_start"
        run_command(__method__, args)
      end

      def oo_stop(args)
        @logger.debug "oo_stoping"
        run_command(__method__, args)
      end

      def oo_restart(args)
        @logger.debug "oo_restart"
        run_command(__method__, args)
      end

      def oo_reload(args)
        @logger.debug "oo_reload"
        run_command(__method__, args)
      end

      def oo_status(args)
        @logger.debug "oo_status"
        run_command(__method__, args)
      end

      def oo_threaddump(args)
        @logger.debug "oo_threaddump"
        #TODO we need to implement this
      end

      #
      # Set the district for a node
      #
      def set_district_action
        @logger.debug "set_district_action"
        uuid = request[:uuid].to_s if request[:uuid]
        active = request[:active]
        first_uid = request[:first_uid]
        max_uid = request[:max_uid]

        args = "-Uuid #{uuid} " if uuid
        args = args + "-Active #{active} " if active
        args = args + "-FirstUid #{first_uid} " if first_uid
        args = args + "-MaxUid #{max_uid} " if max_uid

        rc, output = run_command(__method__, args)
        reply[:output] = output
        reply[:exitcode] = rc

      end

      def set_district_uid_limits_action
        @logger.debug "set_district_action"
        first_uid = request[:first_uid]
        args = "-FirstUid #{first_uid} -MaxUid #{max_uid}"

        rc, output = run_command(__method__, args)
        reply[:output] = output
        reply[:exitcode] = rc
      end

      #
      # Returns whether an app is on a server
      #
      def has_app_action
        @logger.debug "has_app_action"
        validate :uuid, /^[a-zA-Z0-9]+$/
        validate :application, /^[a-zA-Z0-9]+$/
        uuid = request[:uuid].to_s if request[:uuid]
        app_name = request[:application]
        if File.exist?("C:/openshift/gears/#{uuid}/#{app_name}")
          #if File.exist?("/var/lib/openshift/#{uuid}/#{app_name}")
          reply[:output] = true
        else
          reply[:output] = false
        end
        reply[:exitcode] = 0
      end

      #
      # Returns whether an embedded app is on a server
      #
      def has_embedded_app_action
        @logger.debug "has_embedded_app_action"
        validate :uuid, /^[a-zA-Z0-9]+$/
        validate :embedded_type, /^.+$/
        uuid = request[:uuid].to_s if request[:uuid]
        embedded_type = request[:embedded_type]
        if File.exist?("C:/openshift/gears/#{uuid}/#{embedded_type}")
          reply[:output] = true
        else
          reply[:output] = false
        end
        reply[:exitcode] = 0
      end

      #
      # Returns the entire set of env variables for a given gear uuid
      #
      def get_gear_envs_action
        @logger.debug "get_gear_envs_action"
        validate :uuid, /^[a-zA-Z0-9]+$/
        args = "-Uuid #{request[:uuid]}"
        rc, output = run_command(__method__, args)
        reply[:output] =  JSON.parse(output)
        reply[:exitcode] = rc
      end

      #
      # Returns whether a uid or gid is already reserved on the system
      #
      def has_uid_or_gid_action
        @logger.debug "has_uid_or_gid_action"
        #TODO get path from config
        uid  = request[:uid]
        uids = IO.readlines("C:/openshift/cygwin/installation/etc/passwd").map { |line| line.split(":")[2].to_i }
        gids = IO.readlines("C:/openshift/cygwin/installation/etc/group").map { |line| line.split(":")[2].to_i }

        if uids.include?(uid) || gids.include?(uid)
          reply[:output] = true
        else
          reply[:output] = false
        end
        reply[:exitcode] = 0
      end

      #
      # Returns whether the cartridge is present on a gear
      #
      def has_app_cartridge_action
        @logger.debug "has_app_cartridge_action"

        app_uuid = request[:app_uuid].to_s if request[:app_uuid]
        gear_uuid = request[:gear_uuid].to_s if request[:gear_uuid]
        cart_name = request[:cartridge]

        args = "-AppUuid #{app_uuid} -GearUuid #{gear_uuid} -CartName #{cart_name}"
        rc, output = run_command(__method__, args)
        reply[:output] = output
        reply[:exitcode] = rc
      end

      #
      # Get all gears
      #
      def get_all_gears_action
        @logger.debug "get_all_gears_action"
        gear_map = {}

        uid_map          = {}
        #TODO get path from config
        uids             = IO.readlines("C:/openshift/cygwin/installation/etc/passwd").map { |line|
          uid               = line.split(":")[2]
          username          = line.split(":")[0]
          uid_map[username] = uid
        }
        dir              = "C:/openshift/gears/"
        filelist         = Dir.foreach(dir) { |file|
          if File.directory?(dir+file) and not File.symlink?(dir+file) and not file[0]=='.'
            if uid_map.has_key?(file)
              if request[:with_broker_key_auth]
                next unless File.exists?(File.join(dir, file, ".auth/token"))
              end

              @logger.debug "file is #{file}"
              @logger.debug "uid_map is #{uid_map[file]}"

              gear_map[file] = uid_map[file]
            end
          end
        }
        reply[:output]   = gear_map
        reply[:exitcode] = 0
      end

      #
      # Get all sshkeys for all gears
      #
      def get_all_gears_sshkeys_action
        @logger.debug "get_all_gears_sshkeys_action"
        gear_map = {}
        #TODO get path from config
        dir              = "c:/openshift/gears/"
        filelist         = Dir.foreach(dir) do |gear_file|
          if File.directory?(dir + gear_file) and not File.symlink?(dir + gear_file) and not gear_file[0] == '.'
            gear_map[gear_file] = {}
            authorized_keys_file = File.join(dir, gear_file, ".ssh", "authorized_keys")
            if File.exists?(authorized_keys_file) and not File.directory?(authorized_keys_file)
              File.open(authorized_keys_file, File::RDONLY) do |key_file|
                key_file.each_line do |line|
                  begin
                    gear_map[gear_file][Digest::MD5.hexdigest(line.split[-2].chomp)] = line.split[-1].chomp
                  rescue
                  end
                end
              end
            end
          end
        end
        reply[:output]   = gear_map
        reply[:exitcode] = 0
      end

      #
      # Get all gears
      #
      def get_all_active_gears_action
        @logger.debug "get_all_active_gears_action"
        active_gears     = {}
        #TODO get path from config
        dir              = "c:/openshift/gears/"
        filelist         = Dir.foreach(dir) { |file|
          if File.directory?(dir+file) and not File.symlink?(dir+file) and not file[0]=='.'
            state_file = File.join(dir, file, 'app-root', 'runtime', '.state')
            if File.exist?(state_file)
              state  = File.read(state_file).chomp
              active = !('idle' == state || 'stopped' == state)
              active_gears[file] = nil if active
            end
          end
        }
        reply[:output]   = active_gears
        reply[:exitcode] = 0
      end

      ## Perform operation on CartridgeRepository
      def cartridge_repository_action
        @logger.debug "cartridge_repository_action"
        # Log.instance.info("action: #{request.action}_action, agent=#{request.agent}, data=#{request.data.pretty_inspect}")
        action            = request[:action]
        path              = request[:path]
        name              = request[:name]
        version           = request[:version]
        cartridge_version = request[:cartridge_version]

        args = "-Action #{action} "
        args = args + "-Path #{path} " if path
        args = args + "-Name #{name} " if name
        args = args + "-Version #{version} " if version
        args = args + "-CartridgeVersion #{cartridge_version}" if cartridge_version
        rc, output =run_command(__method__, args)
        reply[:output] = output
        reply[:exitcode] = rc
      end

      def oo_user_var_add(args)
        variables = {}
        vars = ""
        if args['--with-variables']
          JSON.parse(args['--with-variables']).each {|env|
            variables[env['name']] = env['value']
            vars = vars + "#{env['name']}=#{env['value']} "
          }

        end
        gears = args['--with-gears'] ? args['--with-gears'].split(';') : []

        if variables.empty? and gears.empty?
          return -1, "In #{__method__} at least user environment variables or gears must be provided for #{args['--with-app-name']}"
        end
        args['--with-variables'] = "#{vars}"

        @logger.debug "ENV_VARIABLES: #{variables}"

        rc, output = run_command(__method__, args)
        return rc, output
      end

      def oo_user_var_remove(args)
        @logger.debug "method is #{__method__.to_s}"

        unless args['--with-keys']
          return -1, "In #{__method__} no user environment variable names provided for #{args['--with-app-name']}"
        end

        exitcode, output = run_command(__method__, args)
        return exitcode, output
        return 0, ''
      end

      def oo_user_var_list(args)
        @logger.debug "oo_user_var_list"
        @logger.debug "method is #{__method__.to_s}"
        exitcode, output = run_command(__method__, args)
        return exitcode, output
      end

      def has_gear_action
        validate :uuid, /^[a-zA-Z0-9]+$/
        uuid = request[:uuid].to_s

        # TODO: the path should be loaded from the node.config file
        if Dir.exist?("c:/openshift/gears/#{uuid}")
          reply[:output] = true
        else
          reply[:output] = false
        end
        reply[:exitcode] = 0
      end

      #
      # Returns the public endpoints of all cartridges on the gear
      #
      def get_all_gears_endpoints_action
        gear_map = {}
        @logger.debug "get_all_gears_endpoints_action"
        rc, output = run_command(__method__, '')

        reply[:output]   =  JSON.parse(output)
        reply[:exitcode] = rc
      end
      def oo_update_cluster(args)
        return 0, ''
      end
    end
  end
end