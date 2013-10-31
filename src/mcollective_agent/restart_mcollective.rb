puts "Stopping mcollective"
`net stop mcollectived > NUL 2> NUL `

puts "Waiting 3 sec for mcollective to exit ..."
sleep 3

puts "Starting mcollective"
`net start mcollectived > NUL 2> NUL `


puts "Waiting 3 sec for mcollective to start ..."
sleep 3

puts "Opening dev page in browser ..."
`start http://127.0.0.1:17171`

puts "Done"
