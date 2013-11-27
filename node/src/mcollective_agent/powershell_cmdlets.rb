require 'open3'
require 'json'

module MCollective
  module Agent
    class Powershell

      def self.print_to_debug(msg)
        $dev_debug_msg << "#{msg} - #{Time.new}"
      end

      def self.run_command(command ,args)
        script = File.join(File.expand_path('../../powershell/oo-cmdlets', __FILE__), "#{command.to_s.gsub('_', '-')}.ps1")
        ps_args = args.to_json.gsub('"', '"""')
        cmd = "powershell.exe -ExecutionPolicy Bypass -InputFormat None -noninteractive -file #{script} #{ps_args} 2>&1"
        output = ""
        exitcode = 0
        Open3.popen3(cmd) do |stdin, stdout, stderr, wait_thr|
          output = stdout.read
          exitcode = wait_thr.value.exitstatus

          print_to_debug "OUT #{command}: ARGS :#{args} STDOUT: #{output} EXITCODE: #{exitcode}"

        end
        return exitcode, output
      end
    end
  end
end
