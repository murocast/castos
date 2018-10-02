fromCategory('subscription')
.when({
    "Castos.Events+CastosEventData+SubscriptionAdded": function(s,e){
			linkTo('latest-subscriptions', e);
			return null;
    }
});