require 'open3'
require 'json'

module MCollective
  module Agent
    class Powershell

      def self.print_to_debug(msg)
        $dev_debug_msg << "<pre>#{msg} - #{Time.new}</pre>"
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


          debug_output = output.gsub(/exception/i, '<span style="color:red">exception</span>')
          debug_output = debug_output.gsub(/~~/i, '<span style="color:red">~~</span>')

          if exitcode != 0
            print_to_debug "<div style='color:red'>OUT #{command}: EXITCODE: #{exitcode} STDOUT: #{debug_output}</div>"
          else
            print_to_debug "<div style='color:green'>OUT #{command}: EXITCODE: #{exitcode} STDOUT: #{debug_output}</div>"
          end
        end
        return exitcode, output
      end
    end
  end
end
